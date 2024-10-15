using System.Buffers;
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
        IShareForCommand share,
        LinkId fileId,
        RevisionId revisionId,
        int index,
        PgpSessionKey contentKey,
        PgpPrivateKey signingKey,
        PgpKey signatureEncryptionKey,
        RecyclableMemoryStream plainDataStream,
        BlockVerifier verifier,
        byte[] plainDataPrefix,
        int plainDataPrefixLength,
        CancellationToken cancellationToken)
    {
        try
        {
            try
            {
                var dataPacketStream = ProtonDriveClient.MemoryStreamManager.GetStream();
                await using (dataPacketStream.ConfigureAwait(false))
                {
                    byte[] sha256Digest;
                    ReadOnlyMemory<byte> signature;

                    await using (plainDataStream.ConfigureAwait(false))
                    {
                        var signatureStream = ProtonDriveClient.MemoryStreamManager.GetStream();

                        await using (signatureStream.ConfigureAwait(false))
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

                            signature = signatureStream.GetBuffer().AsMemory()[..(int)signatureStream.Length];
                            sha256Digest = sha256.Hash ?? [];
                        }
                    }

                    var verificationToken = verifier.VerifyBlock(dataPacketStream.GetFirstBytes(128), plainDataPrefix.AsSpan()[..plainDataPrefixLength]);

                    var parameters = new BlockUploadRequestParameters
                    {
                        AddressId = share.MembershipAddressId.Value,
                        ShareId = share.Id.Value,
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

                    var uploadRequestResponse = await _client.FilesApi.RequestBlockUploadAsync(parameters, cancellationToken).ConfigureAwait(false);

                    dataPacketStream.Seek(0, SeekOrigin.Begin);

                    await _client.StorageApi.UploadBlobAsync(uploadRequestResponse.UploadUrls[0].Value, dataPacketStream, cancellationToken)
                        .ConfigureAwait(false);

                    return sha256Digest;
                }
            }
            finally
            {
                BlockSemaphore.Release();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(plainDataPrefix);
        }
    }
}
