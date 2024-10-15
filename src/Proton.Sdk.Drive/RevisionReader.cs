using System.Runtime.CompilerServices;
using Proton.Cryptography.Pgp;
using Proton.Sdk.Drive.Files;

namespace Proton.Sdk.Drive;

public sealed class RevisionReader : IDisposable
{
    private const int MinBlockIndex = 1;
    private const int BlockPageSize = 15;

    private readonly ProtonDriveClient _client;
    private readonly ShareId _shareId;
    private readonly INodeIdentity _file;
    private readonly IRevisionForTransfer _revision;
    private readonly PgpSessionKey _contentKey;

    private bool _semaphoreReleased;

    internal RevisionReader(ProtonDriveClient client, ShareId shareId, INodeIdentity file, IRevisionForTransfer revision, PgpSessionKey contentKey)
    {
        _client = client;
        _shareId = shareId;
        _file = file;
        _revision = revision;
        _contentKey = contentKey;
    }

    public async Task<VerificationStatus> ReadAsync(Stream contentOutputStream, CancellationToken cancellationToken)
    {
        var downloadTasks = new Queue<Task<BlockDownloadResult>>(_client.BlockDownloader.MaxDegreeOfParallelism);
        var manifestStream = ProtonDriveClient.MemoryStreamManager.GetStream();

        await using (manifestStream)
        {
            foreach (var sha256Digest in _revision.SamplesSha256Digests)
            {
                manifestStream.Write(sha256Digest.Span);
            }

            try
            {
                try
                {
                    await foreach (var (block, _) in GetBlocksAsync(cancellationToken))
                    {
                        if (!await _client.BlockDownloader.BlockSemaphore.WaitAsync(0, cancellationToken).ConfigureAwait(false))
                        {
                            if (downloadTasks.Count > 0)
                            {
                                await WriteNextBlockAsync(downloadTasks, contentOutputStream, manifestStream, cancellationToken).ConfigureAwait(false);
                            }

                            await _client.BlockDownloader.BlockSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                        }

                        var downloadTask = DownloadBlockAsync(block, contentOutputStream, cancellationToken);

                        downloadTasks.Enqueue(downloadTask);
                    }
                }
                finally
                {
                    _client.BlockDownloader.FileSemaphore.Release();
                    _semaphoreReleased = true;
                }

                while (downloadTasks.Count > 0)
                {
                    await WriteNextBlockAsync(downloadTasks, contentOutputStream, manifestStream, cancellationToken).ConfigureAwait(false);
                }
            }
            catch when (downloadTasks.Count > 0)
            {
                try
                {
                    await Task.WhenAll(downloadTasks).ConfigureAwait(false);
                }
                finally
                {
                    _client.BlockDownloader.BlockSemaphore.Release(downloadTasks.Count);
                }

                throw;
            }

            manifestStream.Seek(0, SeekOrigin.Begin);
            return (VerificationStatus)await VerifyManifestAsync(manifestStream, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        if (!_semaphoreReleased)
        {
            _client.BlockDownloader.FileSemaphore.Release();
        }
    }

    private async Task WriteNextBlockAsync(
        Queue<Task<BlockDownloadResult>> downloadTasks,
        Stream outputStream,
        Stream manifestStream,
        CancellationToken cancellationToken)
    {
        var downloadTask = downloadTasks.Dequeue();

        try
        {
            var downloadResult = await downloadTask.ConfigureAwait(false);

            manifestStream.Write(downloadResult.Sha256Digest.Span);

            if (!downloadResult.IsIntermediateStream)
            {
                return;
            }

            var downloadedStream = downloadResult.Stream;

            await using (downloadResult.Stream.ConfigureAwait(false))
            {
                downloadedStream.Seek(0, SeekOrigin.Begin);

                await downloadedStream.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _client.BlockDownloader.BlockSemaphore.Release();
        }
    }

    private async Task<BlockDownloadResult> DownloadBlockAsync(Block block, Stream contentOutputStream, CancellationToken cancellationToken)
    {
        Stream blockOutputStream;
        bool isIntermediateStream;

        if (block.Index == 1)
        {
            blockOutputStream = contentOutputStream;
            isIntermediateStream = false;
        }
        else
        {
            blockOutputStream = ProtonDriveClient.MemoryStreamManager.GetStream();
            isIntermediateStream = true;
        }

        var hash = await _client.BlockDownloader.DownloadAsync(block.Url, _contentKey, blockOutputStream, cancellationToken).ConfigureAwait(false);

        return new BlockDownloadResult(blockOutputStream, isIntermediateStream, hash);
    }

    private async IAsyncEnumerable<(Block Value, bool IsLast)> GetBlocksAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var mustTryNextPageOfBlocks = true;
        var lastKnownIndex = MinBlockIndex - 1;
        var nextExpectedIndex = 1;
        var outstandingBlock = default(Block);
        var currentPageBlocks = new List<Block>(BlockPageSize);

        while (mustTryNextPageOfBlocks)
        {
            currentPageBlocks.Clear();

            var revisionResponse =
                await _client.FilesApi.GetRevisionAsync(
                    _shareId,
                    _file.Id,
                    _revision.Id,
                    lastKnownIndex + 1,
                    BlockPageSize,
                    false,
                    cancellationToken).ConfigureAwait(false);

            var revision = revisionResponse.Revision;

            cancellationToken.ThrowIfCancellationRequested();

            if (revision.Blocks.Count == 0)
            {
                break;
            }

            mustTryNextPageOfBlocks = revision.Blocks.Count >= BlockPageSize;

            currentPageBlocks.AddRange(revision.Blocks);
            currentPageBlocks.Sort((a, b) => a.Index.CompareTo(b.Index));

            var blocksExceptLast = currentPageBlocks.Take(currentPageBlocks.Count - 1);
            var blocksToReturn = outstandingBlock is not null ? blocksExceptLast.Prepend(outstandingBlock) : blocksExceptLast;

            outstandingBlock = currentPageBlocks[^1];
            lastKnownIndex = outstandingBlock.Index;

            foreach (var block in blocksToReturn)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (block.Index != nextExpectedIndex)
                {
                    throw new ProtonApiException($"Missing block index {nextExpectedIndex}");
                }

                ++nextExpectedIndex;

                yield return (block, false);
            }
        }

        if (outstandingBlock is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return (outstandingBlock, true);
        }
    }

    private async Task<PgpVerificationStatus> VerifyManifestAsync(Stream manifestStream, CancellationToken cancellationToken)
    {
        if (_revision.ManifestSignature is null)
        {
            return PgpVerificationStatus.NotSigned;
        }

        if (string.IsNullOrEmpty(_revision.SignatureEmailAddress))
        {
            return PgpVerificationStatus.NoVerifier;
        }

        var verificationKeys = await _client.Account.GetAddressPublicKeysAsync(_revision.SignatureEmailAddress, cancellationToken).ConfigureAwait(false);

        if (verificationKeys.Count == 0)
        {
            return PgpVerificationStatus.NoVerifier;
        }

        var verificationResult = new PgpKeyRing(verificationKeys).Verify(manifestStream, _revision.ManifestSignature.Value.Span);

        return verificationResult.Status;
    }

    private readonly struct BlockDownloadResult(Stream stream, bool isIntermediateStream, ReadOnlyMemory<byte> sha256Digest)
    {
        public Stream Stream { get; } = stream;
        public bool IsIntermediateStream { get; } = isIntermediateStream;
        public ReadOnlyMemory<byte> Sha256Digest { get; } = sha256Digest;
    }
}
