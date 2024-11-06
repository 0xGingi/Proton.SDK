using System.Buffers;
using System.Text.Json;
using Microsoft.IO;
using Proton.Cryptography.Pgp;
using Proton.Sdk.Drive.Files;
using Proton.Sdk.Drive.Serialization;
using Proton.Sdk.Drive.Verification;

namespace Proton.Sdk.Drive;

public sealed class RevisionWriter : IDisposable
{
    public const int DefaultBlockSize = 1 << 22; // 4 MiB

    private readonly ProtonDriveClient _client;
    private readonly ShareMetadata _shareMetadata;
    private readonly LinkId _fileId;
    private readonly RevisionId _revisionId;
    private readonly PgpPrivateKey _fileKey;
    private readonly PgpSessionKey _contentKey;
    private readonly PgpPrivateKey _signingKey;

    private readonly int _targetBlockSize;
    private readonly int _maxBlockSize;

    private bool _semaphoreReleased;
    private long _numberOfBytesUploaded;

    internal RevisionWriter(
        ProtonDriveClient client,
        ShareMetadata shareMetadata,
        LinkId fileId,
        RevisionId revisionId,
        PgpPrivateKey fileKey,
        PgpSessionKey contentKey,
        PgpPrivateKey signingKey,
        int targetBlockSize = DefaultBlockSize,
        int maxBlockSize = DefaultBlockSize)
    {
        _client = client;
        _shareMetadata = shareMetadata;
        _fileId = fileId;
        _revisionId = revisionId;
        _fileKey = fileKey;
        _contentKey = contentKey;
        _signingKey = signingKey;
        _targetBlockSize = targetBlockSize;
        _maxBlockSize = maxBlockSize;
    }

    public event Action<long>? ProgressUpdated;

    public async Task WriteAsync(
        Stream contentInputStream,
        IEnumerable<FileSample> samples,
        DateTimeOffset? lastModificationTime,
        CancellationToken cancellationToken)
    {
        var signinEmailAddress = _shareMetadata.MembershipEmailAddress;

        var uploadTasks = new Queue<Task<byte[]>>(_client.BlockUploader.MaxDegreeOfParallelism);
        var blockIndex = 0;

        // TODO: provide capacity
        var manifestStream = ProtonDriveClient.MemoryStreamManager.GetStream();

        ArraySegment<byte> manifestSignature;
        var blockSizes = new List<int>(8);

        await using (manifestStream.ConfigureAwait(false))
        {
            var blockVerifier = await BlockVerifier.CreateAsync(_client.RevisionVerificationApi, _shareMetadata.ShareId, _fileId, _revisionId, _fileKey, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                try
                {
                    foreach (var sample in samples)
                    {
                        await WaitForBlockUploaderAsync(uploadTasks, manifestStream, cancellationToken).ConfigureAwait(false);

                        var uploadTask = _client.BlockUploader.UploadAsync(_shareMetadata, _fileId, _revisionId, _contentKey, _signingKey, sample, cancellationToken);

                        uploadTasks.Enqueue(uploadTask);
                    }

                    do
                    {
                        var plainDataPrefix = ArrayPool<byte>.Shared.Rent(blockVerifier.DataPacketPrefixMaxLength);
                        try
                        {
                            var plainDataStream = ProtonDriveClient.MemoryStreamManager.GetStream();

                            await contentInputStream.PartiallyCopyToAsync(plainDataStream, _targetBlockSize, plainDataPrefix, cancellationToken)
                                .ConfigureAwait(false);

                            blockSizes.Add((int)plainDataStream.Length);

                            await WaitForBlockUploaderAsync(uploadTasks, manifestStream, cancellationToken).ConfigureAwait(false);

                            plainDataStream.Seek(0, SeekOrigin.Begin);

                            var uploadTask = _client.BlockUploader.UploadAsync(
                                _shareMetadata,
                                _fileId,
                                _revisionId,
                                ++blockIndex,
                                _contentKey,
                                _signingKey,
                                _fileKey,
                                plainDataStream,
                                blockVerifier,
                                plainDataPrefix,
                                (int)Math.Min(blockVerifier.DataPacketPrefixMaxLength, plainDataStream.Length),
                                (progress) =>
                                {
                                    _numberOfBytesUploaded += progress;
                                    ProgressUpdated?.Invoke(_numberOfBytesUploaded);
                                },
                                cancellationToken);

                            uploadTasks.Enqueue(uploadTask);
                        }
                        catch
                        {
                            ArrayPool<byte>.Shared.Return(plainDataPrefix);
                            throw;
                        }
                    } while (contentInputStream.Position < contentInputStream.Length);

                    // TODO: upload samples
                }
                finally
                {
                    _client.BlockUploader.FileSemaphore.Release();
                    _semaphoreReleased = true;
                }

                while (uploadTasks.Count > 0)
                {
                    await AddNextBlockToManifestAsync(uploadTasks, manifestStream).ConfigureAwait(false);
                }
            }
            catch when (uploadTasks.Count > 0)
            {
                try
                {
                    await Task.WhenAll(uploadTasks).ConfigureAwait(false);
                }
                finally
                {
                    _client.BlockUploader.BlockSemaphore.Release(uploadTasks.Count);
                    _client.RevisionCreationSemaphore.Release(uploadTasks.Count);
                }

                throw;
            }

            manifestStream.Seek(0, SeekOrigin.Begin);

            manifestSignature = await _signingKey.SignAsync(manifestStream, cancellationToken).ConfigureAwait(false);
        }

        var parameters = GetRevisionUpdateParameters(contentInputStream, lastModificationTime, blockSizes, manifestSignature, signinEmailAddress);

        await _client.FilesApi.UpdateRevisionAsync(_shareMetadata.ShareId, _fileId, _revisionId, parameters, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (!_semaphoreReleased)
        {
            _client.BlockUploader.FileSemaphore.Release();
        }
    }

    private static async Task AddNextBlockToManifestAsync(Queue<Task<byte[]>> uploadTasks, RecyclableMemoryStream manifestStream)
    {
        var sha256Digest = await uploadTasks.Dequeue().ConfigureAwait(false);
        manifestStream.Write(sha256Digest);
    }

    private async ValueTask WaitForBlockUploaderAsync(Queue<Task<byte[]>> uploadTasks, RecyclableMemoryStream manifestStream, CancellationToken cancellationToken)
    {
        if (!await _client.BlockUploader.BlockSemaphore.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            if (uploadTasks.Count > 0)
            {
                await AddNextBlockToManifestAsync(uploadTasks, manifestStream).ConfigureAwait(false);
            }

            await _client.BlockUploader.BlockSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private RevisionUpdateParameters GetRevisionUpdateParameters(
        Stream contentInputStream,
        DateTimeOffset? lastModificationTime,
        IReadOnlyList<int> blockSizes,
        ArraySegment<byte> manifestSignature,
        string signinEmailAddress)
    {
        var extendedAttributes = new ExtendedAttributes
        {
            Common = new CommonExtendedAttributes
            {
                Size = contentInputStream.Length,
                ModificationTime = lastModificationTime?.UtcDateTime,
                BlockSizes = blockSizes,
            },
        };

        var extendedAttributesUtf8Bytes = JsonSerializer.SerializeToUtf8Bytes(extendedAttributes, ProtonDriveApiSerializerContext.Default.ExtendedAttributes);

        var encryptedExtendedAttributes = _fileKey.EncryptAndSign(extendedAttributesUtf8Bytes, _signingKey, outputCompression: PgpCompression.Default);

        return new RevisionUpdateParameters
        {
            ManifestSignature = manifestSignature,
            SignatureEmailAddress = signinEmailAddress,
            ExtendedAttributes = encryptedExtendedAttributes,
        };
    }
}
