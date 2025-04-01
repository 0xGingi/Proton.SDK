using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using Microsoft.IO;
using Proton.Cryptography.Pgp;
using Proton.Sdk.Drive.Files;
using Proton.Sdk.Drive.Verification;

namespace Proton.Sdk.Drive;

internal sealed class BlockUploader
{
    private readonly ProtonDriveClient _client;

    internal BlockUploader(ProtonDriveClient client, int maxDegreeOfParallelism)
    {
        _client = client;
        MaxDegreeOfParallelism = maxDegreeOfParallelism;
        BlockSemaphore = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);
    }

    public int MaxDegreeOfParallelism { get; }

    public SemaphoreSlim FileSemaphore { get; } = new(1, 1);
    public SemaphoreSlim BlockSemaphore { get; }

    public async Task<byte[]> UploadAsync(
        ShareMetadata shareMetadata,
        LinkId fileId,
        RevisionId revisionId,
        int index,
        PgpSessionKey contentKey,
        PgpPrivateKey signingKey,
        PgpKey signatureEncryptionKey,
        Stream plainDataStream,
        BlockVerifier verifier,
        byte[] plainDataPrefix,
        int plainDataPrefixLength,
        Action<long> onBlockProgress,
        Action<int> releaseBlocksAction,
        CancellationToken cancellationToken)
    {
        try
        {
            try
            {
                var dataPacketStream = ProtonDriveClient.MemoryStreamManager.GetStream();
                await using (dataPacketStream.ConfigureAwait(false))
                {
                    var signatureStream = ProtonDriveClient.MemoryStreamManager.GetStream();

                    await using (signatureStream.ConfigureAwait(false))
                    {
                        byte[] sha256Digest;

                        await using (plainDataStream.ConfigureAwait(false))
                        {
                            using var sha256 = SHA256.Create();

                            var hashingStream = new CryptoStream(dataPacketStream, sha256, CryptoStreamMode.Write, leaveOpen: true);

                            await using (hashingStream.ConfigureAwait(false))
                            {
                                var signatureEncryptingStream = signatureEncryptionKey.OpenEncryptingStream(signatureStream);

                                await using (signatureEncryptingStream.ConfigureAwait(false))
                                {
                                    var encryptingStream = contentKey.OpenEncryptingAndSigningStream(hashingStream, signatureEncryptingStream, signingKey);

                                    await using (encryptingStream.ConfigureAwait(false))
                                    {
                                        await plainDataStream.CopyToAsync(encryptingStream, cancellationToken).ConfigureAwait(false);
                                    }
                                }
                            }

                            sha256Digest = sha256.Hash ?? [];
                        }

                        // The signature stream should not be closed until the signature is no longer needed, because the underlying buffer could be re-used,
                        // leading to a garbage signature.
                        var signature = signatureStream.GetBuffer().AsMemory()[..(int)signatureStream.Length];

                        var verificationToken = verifier.VerifyBlock(dataPacketStream.GetFirstBytes(128), plainDataPrefix.AsSpan()[..plainDataPrefixLength]);

                        var parameters = new BlockUploadRequestParameters
                        {
                            AddressId = shareMetadata.MembershipAddressId.Value,
                            ShareId = shareMetadata.ShareId.Value,
                            LinkId = fileId.Value,
                            RevisionId = revisionId.Value,
                            Blocks =
                            [
                                new BlockCreationParameters
                            {
                                Index = index,
                                Size = (int)dataPacketStream.Length,
                                HashDigest = sha256Digest,
                                EncryptedSignature = signature,
                                VerifierOutput = new BlockVerifierOutput { Token = verificationToken.AsReadOnlyMemory() },
                            },
                        ],
                            Thumbnails = [],
                        };

                        await UploadBlobAsync(parameters, dataPacketStream, cancellationToken).ConfigureAwait(false);

                        onBlockProgress.Invoke(dataPacketStream.Position);

                        return sha256Digest;
                    }
                }
            }
            finally
            {
                try
                {
                    BlockSemaphore.Release();
                }
                finally
                {
                    releaseBlocksAction.Invoke(1);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(plainDataPrefix);
        }
    }

    public async Task<byte[]> UploadAsync(
        IShareForCommand share,
        LinkId fileId,
        RevisionId revisionId,
        PgpSessionKey contentKey,
        PgpPrivateKey signingKey,
        FileSample sample,
        CancellationToken cancellationToken)
    {
        try
        {
            var dataPacketStream = ProtonDriveClient.MemoryStreamManager.GetStream();
            await using (dataPacketStream.ConfigureAwait(false))
            {
                using var sha256 = SHA256.Create();

                var hashingStream = new CryptoStream(dataPacketStream, sha256, CryptoStreamMode.Write, leaveOpen: true);

                await using (hashingStream.ConfigureAwait(false))
                {
                    var encryptingStream = contentKey.OpenEncryptingAndSigningStream(hashingStream, signingKey);

                    await using (encryptingStream.ConfigureAwait(false))
                    {
                        encryptingStream.Write(sample.Content);
                    }
                }

                var sha256Digest = sha256.Hash ?? [];

                var parameters = new BlockUploadRequestParameters
                {
                    AddressId = share.MembershipAddressId.Value,
                    ShareId = share.ShareId.Value,
                    LinkId = fileId.Value,
                    RevisionId = revisionId.Value,
                    Blocks = [],
                    Thumbnails =
                    [
                        new ThumbnailCreationParameters
                        {
                            Size = (int)dataPacketStream.Length,
                            Type = (ThumbnailType)sample.Type,
                            HashDigest = sha256Digest,
                        },
                    ],
                };

                await UploadBlobAsync(parameters, dataPacketStream, cancellationToken).ConfigureAwait(false);

                return sha256Digest;
            }
        }
        finally
        {
            BlockSemaphore.Release();
            _client.RevisionCreationSemaphore.Release(1);
        }
    }

    private async Task UploadBlobAsync(BlockUploadRequestParameters parameters, RecyclableMemoryStream dataPacketStream, CancellationToken cancellationToken)
    {
        Debug.Assert(parameters.Thumbnails.Count + parameters.Blocks.Count == 1, "Blob upload request should request only one block (content or thumbnail)");

        var remainingNumberOfAttempts = 2;

        while (remainingNumberOfAttempts > 0)
        {
            try
            {
                var uploadRequestResponse = await _client.FilesApi.RequestBlockUploadAsync(parameters, cancellationToken).ConfigureAwait(false);

                var uploadTarget = parameters.Thumbnails.Count == 0 ? uploadRequestResponse.UploadTargets[0] : uploadRequestResponse.ThumbnailUploadTargets[0];
                var uploadTargetUrl = $"{uploadTarget.BareUrl}/{uploadTarget.Token}";

                dataPacketStream.Seek(0, SeekOrigin.Begin);

                await _client.StorageApi.UploadBlobAsync(uploadTargetUrl, dataPacketStream, cancellationToken).ConfigureAwait(false);

                remainingNumberOfAttempts = 0;
            }
            catch (Exception e) when (UrlExpired(e) || BlobAlreadyUploaded(e))
            {
                --remainingNumberOfAttempts;
            }
        }

        return;

        static bool UrlExpired(Exception e) => e is HttpRequestException { StatusCode: HttpStatusCode.NotFound };

        // This can happen if the previous successful upload response was not processed (e.g. connection interrupted just as the success was being sent back)
        static bool BlobAlreadyUploaded(Exception e) => e is ProtonApiException { Code: ResponseCode.AlreadyExists };
    }
}
