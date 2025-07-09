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
        catch (Exception ex)
        {
            Console.WriteLine("Exception caught while trying to create a ProtonDriveClient: " + ex);
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
        catch (Exception ex)
        {
            Console.WriteLine($"Exception caught while registering share key... {ex}");
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
        catch (Exception ex)
        {
            Console.WriteLine($"Exception caught while registering node key... {ex}");
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
        catch (Exception ex)
        {
            Console.WriteLine($"Exception caught while fetching volumes... {ex}");
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
            // Console.WriteLine("Returning successful response");
            return InteropArray.FromMemory(bytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception caught while fetching shares... {ex}");
            return InteropArray.FromMemory(Array.Empty<byte>());
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "drive_client_get_folder_children", CallConvs = [typeof(CallConvCdecl)])]
    public static InteropArray NativeGetFolderChildren(nint clientHandle, InteropArray nodeIdentityBytes, nint cancellationTokenSourceHandle)
    {
        try
        {
            if (!TryGetFromHandle(clientHandle, out var client))
                return InteropArray.FromMemory(Array.Empty<byte>());

            if (!InteropCancellationTokenSource.TryGetTokenFromHandle(cancellationTokenSourceHandle, out var token))
                return InteropArray.FromMemory(Array.Empty<byte>());

            var nodeIdentity = NodeIdentity.Parser.ParseFrom(nodeIdentityBytes.AsReadOnlySpan());

            try
            {
                Node.GetKeyAsync(client, nodeIdentity, token).GetAwaiter().GetResult();
                // Console.WriteLine($"[Interop] Ensure node key for nodeId={nodeIdentity.NodeId} in volumeId={nodeIdentity.VolumeId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Interop] Failed to ensure node key: {ex}");
            }

            // Log secrets cache miss requests
            if (client.SecretsCache is not null)
            {
                var originalCache = client.SecretsCache;
                client.SecretsCache = new DebugSecretsCache(originalCache);
            }
            else
            {
                Console.WriteLine("[Interop] Secrets cache is null");
            }

            // Console.WriteLine($"[Interop] Listing children for nodeId={nodeIdentity.NodeId} in volumeId={nodeIdentity.VolumeId}");

            List<NodeType> nodes;
            try
            {
                nodes = client.GetFolderChildrenAsync(nodeIdentity, token, includeHidden: true)
                    .ToBlockingEnumerable()
                    .Select(n =>
                    {
                        var node = new NodeType();
                        switch (n)
                        {
                            case FileNode file:
                                node.FileNode = new FileNode
                                {
                                    NodeIdentity = file.NodeIdentity,
                                    ParentId = file.ParentId,
                                    Name = file.Name,
                                    NameHashDigest = file.NameHashDigest,
                                    State = file.State,
                                    ActiveRevision = file.ActiveRevision
                                };
                                break;
                            case FolderNode folder:
                                node.FolderNode = new FolderNode
                                {
                                    NodeIdentity = folder.NodeIdentity,
                                    ParentId = folder.ParentId,
                                    Name = folder.Name,
                                    NameHashDigest = folder.NameHashDigest,
                                    State = folder.State
                                };
                                break;
                        }

                        return node;
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"[Interop] Exception while enumerating children: {ex}");
                return InteropArray.FromMemory(Array.Empty<byte>());
            }

            // Console.WriteLine($"[Interop] Found {nodes.Count} children");
            if (nodes.Count == 0)
            {
                // Console.WriteLine("[Interop] No children found for this folder.");
            }

            var nodeTypeList = new NodeTypeList();
            nodeTypeList.Nodes.AddRange(nodes);

            var bytes = nodeTypeList.ToByteArray();
            return InteropArray.FromMemory(bytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception caught while fetching folder children... {ex}");
            return InteropArray.FromMemory(Array.Empty<byte>());
        }
    }
}
