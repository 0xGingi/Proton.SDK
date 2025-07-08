using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Google.Protobuf;
using Proton.Sdk.CExports;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Instrumentation.CExport;
using Proton.Sdk.Instrumentation.Provider;

namespace Proton.Sdk.Drive.CExports;

internal static class InteropProtonDriveClient
{
    internal static bool TryGetFromHandle(nint handle, [MaybeNullWhen(false)] out ProtonDriveClient client)
    {
        if (handle == 0)
        {
            client = null;
            return false;
        }

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

            ObservabilityService? observabilityService;
            if (observabilityServiceHandle == 0)
            {
                // Observability should not be used
                observabilityService = null;
            }
            else if (!InteropProtonObservabilityService.TryGetFromHandle(observabilityServiceHandle, out observabilityService))
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

    [UnmanagedCallersOnly(EntryPoint = "drive_client_register_share_key", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeRegisterShareKey(nint handle, InteropArray requestBytes)
    {
        try
        {
            if (!TryGetFromHandle(handle, out var client))
            {
                return -1;
            }

            var request = ShareKeyRegistrationRequest.Parser.ParseFrom(requestBytes.AsReadOnlySpan());
            var shareKeyCacheKey = Share.GetShareKeyCacheKey(new ShareId(request.ShareId.Value));
            client.SecretsCache.Set(shareKeyCacheKey, request.ShareKeyRawUnlockedData.Span);

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "drive_client_register_node_keys", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeRegisterNodeKeys(nint handle, InteropArray requestBytes)
    {
        try
        {
            if (!TryGetFromHandle(handle, out var client))
            {
                return -1;
            }

            var request = NodeKeysRegistrationRequest.Parser.ParseFrom(requestBytes.AsReadOnlySpan());
            client.SecretsCache.Set(Node.GetNodeKeyCacheKey(request.NodeIdentity.VolumeId, request.NodeIdentity.NodeId), request.NodeKeyRawUnlockedData.Span);

            if (request.ContentKeyRawUnlockedData?.IsEmpty == false)
            {
                client.SecretsCache.Set(
                    FileNode.GetContentKeyCacheKey(request.NodeIdentity.VolumeId, request.NodeIdentity.NodeId),
                    request.ContentKeyRawUnlockedData.Span);
            }

            if (request.HashKeyRawUnlockedData?.IsEmpty == false)
            {
                client.SecretsCache.Set(
                    FolderNode.GetHashKeyCacheKey(request.NodeIdentity.VolumeId, request.NodeIdentity.NodeId),
                    request.HashKeyRawUnlockedData.Span);
            }

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

            if (gcHandle.Target is not ProtonDriveClient)
            {
                return;
            }

            gcHandle.Free();
        }
        catch
        {
            // Ignore
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "drive_client_get_volumes", CallConvs = [typeof(CallConvCdecl)])]
    public static InteropArray NativeGetVolumes(nint clientHandle, nint cancellationTokenSourceHandle)
    {
        try
        {
            if (!TryGetFromHandle(clientHandle, out var client))
                return InteropArray.FromMemory(Array.Empty<byte>());

            InteropCancellationTokenSource.TryGetTokenFromHandle(cancellationTokenSourceHandle, out var token);

            var volumes = client.GetVolumesAsync(token).GetAwaiter().GetResult();

            var response = new VolumesResponse();
            response.Volumes.AddRange(volumes.Select(v => new VolumeMetadata
            {
                VolumeId = new VolumeId { Value = v.Id.Value },
                State = v.State,
                MaxSpace = v.MaxSpace ?? 0,
                RootShareId = v.RootShareId
            }));

            var bytes = response.ToByteArray();
            return InteropArray.FromMemory(bytes);
        }
        catch
        {
            return InteropArray.FromMemory(Array.Empty<byte>());
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "drive_client_get_shares", CallConvs = [typeof(CallConvCdecl)])]
    public static InteropArray NativeGetShare(nint clientHandle, InteropArray volumes, nint cancellationTokenSourceHandle) // Returns Share in drive.proto
    {
        // var share = await client.GetShareAsync(mainVolume.RootShareId, cancellationToken);
        try
        {
            if (!TryGetFromHandle(clientHandle, out var client))
            {
                return InteropArray.FromMemory(Array.Empty<byte>());
            }

            var cancellationToken =
                InteropCancellationTokenSource.TryGetTokenFromHandle(cancellationTokenSourceHandle, out var token);
            if (!cancellationToken)
            {
                return InteropArray.FromMemory(Array.Empty<byte>());
            }

            VolumeMetadata volumeMetadata = VolumeMetadata.Parser.ParseFrom(volumes.AsReadOnlySpan());
            var share = client.GetShareAsync(volumeMetadata.RootShareId, token).GetAwaiter().GetResult();

            var response = new Share
            {
                ShareId = share.ShareId,
                MembershipAddressId = share.MembershipAddressId,
                MembershipEmailAddress = share.MembershipEmailAddress,
                VolumeId = volumeMetadata.VolumeId,
                RootNodeId = share.RootNodeId // this changes for each folder...
            };
            var bytes = response.ToByteArray();
            Console.WriteLine("Returning successful response");
            return InteropArray.FromMemory(bytes);
        }
        catch
        {
            Console.WriteLine("Error in NativeGetShare, returning empty array");
            return InteropArray.FromMemory(Array.Empty<byte>());
        }
    }
}
