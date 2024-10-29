using System.Buffers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Proton.Sdk.Http;

internal sealed class TlsRemoteCertificateValidator
{
    private static readonly IReadOnlyCollection<byte[]> KnownPublicKeyHashDigests =
    [
        Convert.FromBase64String("CT56BhOTmj5ZIPgb/xD5mH8rY3BLo/MlhP7oPyJUEDo="),
        Convert.FromBase64String("35Dx28/uzN3LeltkCBQ8RHK0tlNSa2kCpCRGNp34Gxc="),
        Convert.FromBase64String("qYIukVc63DEITct8sFT7ebIq5qsWmuscaIKeJx+5J5A="),
    ];

    public static bool Validate(X509Certificate? certificate, X509Chain? chain)
    {
        if (certificate == null || chain == null)
        {
            return false;
        }

        var certificateIsValid = IsValid(certificate);

        // TODO: TLS pinning report

        // Ignore other potential SSL policy errors if the certificate is valid.
        return certificateIsValid;
    }

    private static bool IsValid(X509Certificate certificate)
    {
        using var certificate2 = new X509Certificate2(certificate);
        Span<byte> hashDigestBuffer = stackalloc byte[256];
        if (!TryGetPublicKeyHashDigest(certificate2, hashDigestBuffer, out var hashDigestLength))
        {
            return false;
        }

        var hashDigest = hashDigestBuffer[..hashDigestLength];

        foreach (var knownPublicKeyHashDigest in KnownPublicKeyHashDigests)
        {
            if (knownPublicKeyHashDigest.AsSpan().SequenceEqual(hashDigest))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetPublicKeyHashDigest(X509Certificate2 certificate, Span<byte> outputBuffer, out int numberOfBytesWritten)
    {
        var publicKey = (AsymmetricAlgorithm?)certificate.GetRSAPublicKey()
            ?? certificate.GetDSAPublicKey()
            ?? throw new NotSupportedException("No supported key algorithm");

        // Expected length of public key info is around 550 bytes
        var publicKeyInfoBuffer = ArrayPool<byte>.Shared.Rent(1024);

        try
        {
            var publishKeyInfo = publicKey.TryExportSubjectPublicKeyInfo(publicKeyInfoBuffer, out var publicKeyInfoLength)
                ? publicKeyInfoBuffer.AsSpan()[..publicKeyInfoLength]
                : publicKey.ExportSubjectPublicKeyInfo();

            return SHA256.TryHashData(publishKeyInfo, outputBuffer, out numberOfBytesWritten);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(publicKeyInfoBuffer);
        }
    }
}
