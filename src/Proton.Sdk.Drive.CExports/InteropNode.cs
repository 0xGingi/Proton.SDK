using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Proton.Cryptography.Pgp;
using Proton.Sdk.CExports;

namespace Proton.Sdk.Drive.CExports;

internal static class InteropNode
{
    [UnmanagedCallersOnly(EntryPoint = "node_decrypt_armored_name", CallConvs = [typeof(CallConvCdecl)])]
    private static int DecryptName(
        nint clientHandle,
        InteropArray shareId,
        InteropArray volumeId,
        InteropArray parentLinkId,
        InteropArray armoredEncryptedName,
        InteropAsyncCallback<InteropArray> callback)
    {
        try
        {
            if (!InteropProtonDriveClient.TryGetFromHandle(clientHandle, out var client))
            {
                return -1;
            }

            callback.InvokeFor(
                ct => DecryptNameAsync(
                    client,
                    new ShareId(shareId.Utf8ToString()),
                    new VolumeId(volumeId.Utf8ToString()),
                    new LinkId(parentLinkId.Utf8ToString()),
                    armoredEncryptedName.ToArray(),
                    ct));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    private static async ValueTask<Result<InteropArray, SdkError>> DecryptNameAsync(
        ProtonDriveClient client,
        ShareId shareId,
        VolumeId volumeId,
        LinkId parentLinkId,
        byte[] armoredEncryptedName,
        CancellationToken cancellationToken)
    {
        try
        {
            using var parentKey = await Node.GetKeyAsync(client, shareId, volumeId, parentLinkId, cancellationToken).ConfigureAwait(false);

            // TODO: verification
            var name = parentKey.DecryptText(armoredEncryptedName, PgpEncoding.AsciiArmor);

            return InteropArray.Utf8FromString(name);
        }
        catch (Exception ex)
        {
            return SdkError.FromException(ex);
        }
    }
}
