using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Proton.Sdk.CExports;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Instrumentation.CExport;

namespace Proton.Sdk.Drive.CExports;

internal static class InteropProtonDriveClient
{
    internal static bool TryGetFromHandle(nint handle, [MaybeNullWhen(false)] out ProtonDriveClient client)
    {
        var gcHandle = GCHandle.FromIntPtr(handle);

        client = gcHandle.Target as ProtonDriveClient;

        return client is not null;
    }

    [UnmanagedCallersOnly(EntryPoint = "drive_client_create", CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int NativeCreate(nint sessionHandle, nint observabilityServiceHandle, InteropArray requestBytes, nint* clientHandle)
    {
        try
        {
            if (!InteropProtonApiSession.TryGetFromHandle(sessionHandle, out var session))
            {
                return -1;
            }

            if (!InteropProtonObservabilityService.TryGetFromHandle(observabilityServiceHandle, out var observabilityService))
            {
                return -1;
            }

            var request = ProtonDriveClientCreateRequest.Parser.ParseFrom(requestBytes.AsReadOnlySpan());

            var options = new ProtonDriveClientOptions
            {
                InstrumentationMeter = observabilityService,
                ClientId = request.ClientId.Value,
            };
            var client = new ProtonDriveClient(session, options);

            *clientHandle = GCHandle.ToIntPtr(GCHandle.Alloc(client));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "drive_client_register_node_keys", CallConvs = [typeof(CallConvCdecl)])]
    private static int RegisterNodeKeys(nint handle, InteropArray requestBytes)
    {
        try
        {
            if (!TryGetFromHandle(handle, out var client))
            {
                return -1;
            }

            var request = NodeKeysRegistrationRequest.Parser.ParseFrom(requestBytes.AsReadOnlySpan());
            client.SecretsCache.Set(Node.GetNodeKeyCacheKey(request.NodeIdentity.VolumeId, request.NodeIdentity.NodeId), request.NodeKeyRawUnlockedData.Span);
            client.SecretsCache.Set(
                Node.GetContentKeyCacheKey(request.NodeIdentity.VolumeId, request.NodeIdentity.NodeId),
                request.ContentKeyRawUnlockedData.Span);

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "drive_client_free", CallConvs = [typeof(CallConvCdecl)])]
    private static void NativeFree(nint handle)
    {
        try
        {
            var gcHandle = GCHandle.FromIntPtr(handle);

            gcHandle.Free();
        }
        catch
        {
            // Ignore
        }
    }
}
