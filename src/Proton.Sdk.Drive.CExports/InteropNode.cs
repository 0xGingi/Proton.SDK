using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Proton.Cryptography.Pgp;
using Proton.Sdk.CExports;
using Proton.Sdk.Cryptography;

namespace Proton.Sdk.Drive.CExports;

internal static class InteropNode
{
    [UnmanagedCallersOnly(EntryPoint = "node_decrypt_armored_name", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeDecryptName(
        nint clientHandle,
        InteropArray nodeNameDecryptionRequestBytes,
        InteropAsyncCallback callback)
    {
        try
        {
            if (!InteropProtonDriveClient.TryGetFromHandle(clientHandle, out var client))
            {
                return -1;
            }

            callback.InvokeFor(
                ct => InteropDecryptNameAsync(client, nodeNameDecryptionRequestBytes, ct));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    private static async ValueTask<Result<InteropArray, InteropArray>> InteropDecryptNameAsync(
        ProtonDriveClient client,
        InteropArray nodeNameDecryptionRequestBytes,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = NodeNameDecryptionRequest.Parser.ParseFrom(nodeNameDecryptionRequestBytes.AsReadOnlySpan());

            using var parentKey = await Node.GetKeyAsync(client, request.NodeIdentity, cancellationToken).ConfigureAwait(false);

            var name = await Node.DecryptNameAsync(
                client,
                request.NodeIdentity.VolumeId,
                request.NodeIdentity.NodeId,
                parentKey,
                new PgpArmoredMessage(PgpArmorDecoder.Decode(Encoding.ASCII.GetBytes(request.ArmoredEncryptedName))),
                request.HasSignatureEmailAddress ? request.SignatureEmailAddress : null,
                secretsCache: null,
                cancellationToken).ConfigureAwait(false);

            return ResultExtensions.Success(name);
        }
        catch (Exception e)
        {
            return ResultExtensions.Failure(e);
        }
    }
}
