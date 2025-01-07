using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Proton.Sdk.CExports;
using Proton.Sdk.Instrumentation.Provider;

namespace Proton.Sdk.Instrumentation.CExport;

internal static class InteropProtonObservabilityService
{
    internal static bool TryGetFromHandle(nint handle, [MaybeNullWhen(false)] out ObservabilityService service)
    {
        if (handle == 0)
        {
            service = null;
            return false;
        }

        var gcHandle = GCHandle.FromIntPtr(handle);

        service = gcHandle.Target as ObservabilityService;

        return service is not null;
    }

    [UnmanagedCallersOnly(EntryPoint = "observability_service_create", CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int NativeCreate(nint sessionHandle, nint* observabilityHandle)
    {
        try
        {
            if (!InteropProtonApiSession.TryGetFromHandle(sessionHandle, out var session))
            {
                return -1;
            }

            var observabilityService = new ObservabilityService(session);

            *observabilityHandle = GCHandle.ToIntPtr(GCHandle.Alloc(observabilityService));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "observability_service_start_new", CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int NativeCreateAndStart(nint sessionHandle, nint* observabilityHandle)
    {
        try
        {
            if (!InteropProtonApiSession.TryGetFromHandle(sessionHandle, out var session))
            {
                return -1;
            }

            var observabilityService = ObservabilityService.StartNew(session);

            *observabilityHandle = GCHandle.ToIntPtr(GCHandle.Alloc(observabilityService));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "observability_service_start", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeStart(nint handle)
    {
        try
        {
            if (!TryGetFromHandle(handle, out var service))
            {
                return -1;
            }

            service.Start();

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "observability_service_stop", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeStop(nint handle)
    {
        try
        {
            if (!TryGetFromHandle(handle, out var service))
            {
                return -1;
            }

            service.Stop();

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "observability_service_flush", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeFlush(nint handle, InteropAsyncCallback callback)
    {
        try
        {
            if (!TryGetFromHandle(handle, out var service))
            {
                return -1;
            }

            return callback.InvokeFor(ct => InteropFlushAsync(service, ct));
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "observability_service_free", CallConvs = [typeof(CallConvCdecl)])]
    private static void NativeFree(nint handle)
    {
        try
        {
            var gcHandle = GCHandle.FromIntPtr(handle);

            if (gcHandle.Target is not ObservabilityService)
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

    private static async ValueTask<Result<InteropArray, InteropArray>> InteropFlushAsync(ObservabilityService service, CancellationToken cancellationToken)
    {
        try
        {
            await service.FlushAsync(cancellationToken).ConfigureAwait(false);

            return ResultExtensions.Success();
        }
        catch (Exception exception)
        {
            return ResultExtensions.Failure(exception);
        }
    }
}
