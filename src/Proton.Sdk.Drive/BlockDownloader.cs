using System.Security.Cryptography;
using Proton.Cryptography.Pgp;

namespace Proton.Sdk.Drive;

internal sealed class BlockDownloader
{
    private readonly ProtonDriveClient _client;

    internal BlockDownloader(ProtonDriveClient client, int maxDegreeOfParallelism)
    {
        _client = client;
        MaxDegreeOfParallelism = maxDegreeOfParallelism;
        BlockSemaphore = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);
    }

    public int MaxDegreeOfParallelism { get; }

    public SemaphoreSlim FileSemaphore { get; } = new(1, 1);
    public SemaphoreSlim BlockSemaphore { get; }

    public async Task<(ReadOnlyMemory<byte> HashDigest, PgpVerificationStatus VerificationStatus)> DownloadAsync(
        string url,
        PgpSessionKey contentKey,
        ReadOnlyMemory<byte>? encryptedSignature,
        PgpPrivateKey signatureDecryptionKey,
        IReadOnlyList<PgpPublicKey>? verificationKeys,
        Stream outputStream,
        CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();

        var blobStream = await _client.StorageApi.GetBlobStreamAsync(url, cancellationToken).ConfigureAwait(false);

        var hashingStream = new CryptoStream(blobStream, sha256, CryptoStreamMode.Read);

        // TODO: use array pool for decrypted signature
        var signature = encryptedSignature is not null ? signatureDecryptionKey.Decrypt(encryptedSignature.Value.Span) : default(ArraySegment<byte>?);

        var verificationKeyRing = new PgpKeyRing(verificationKeys ?? []);

        PgpVerificationStatus verificationStatus;

        await using (hashingStream.ConfigureAwait(false))
        {
            var decryptingStream = signature is not null
                ? contentKey.OpenDecryptingAndVerifyingStream(hashingStream, signature.Value, verificationKeyRing)
                : contentKey.OpenDecryptingStream(hashingStream);

            await using (decryptingStream.ConfigureAwait(false))
            {
                await decryptingStream.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);

                using var verificationResult = decryptingStream.GetVerificationResult();

                verificationStatus = verificationResult.Status;
            }
        }

        sha256.TransformFinalBlock([], 0, 0);

        return (sha256.Hash, verificationStatus);
    }
}
