using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Proton.Cryptography.Pgp;
using Proton.Sdk.CExports;

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
            var nodeNameDecryptionRequest = NodeNameDecryptionRequest.Parser.ParseFrom(nodeNameDecryptionRequestBytes.AsReadOnlySpan());

            using var parentKey = await Node.GetKeyAsync(
                client,
                nodeNameDecryptionRequest.NodeIdentity,
                cancellationToken).ConfigureAwait(false);

            // TODO: verification
            var name = parentKey.DecryptText(Encoding.UTF8.GetBytes(nodeNameDecryptionRequest.ArmoredEncryptedName).AsMemory().Span, PgpEncoding.AsciiArmor);

            return ResultExtensions.Success(name);
        }
        catch (Exception e)
        {
            return ResultExtensions.Failure(e);
        }
    }
}
