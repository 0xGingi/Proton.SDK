using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Proton.Sdk.CExports;
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
    private static unsafe int NativeCreate(nint sessionHandle, nint observabilityServiceHandle, nint* clientHandle)
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

            var client = new ProtonDriveClient(session, new ProtonDriveClientOptions { InstrumentFactory = observabilityService });

            *clientHandle = GCHandle.ToIntPtr(GCHandle.Alloc(client));

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
