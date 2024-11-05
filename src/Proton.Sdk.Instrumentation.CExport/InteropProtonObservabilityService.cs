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
        var gcHandle = GCHandle.FromIntPtr(handle);

        service = gcHandle.Target as ObservabilityService;

        return service is not null;
    }

    [UnmanagedCallersOnly(EntryPoint = "observability_service_create", CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int Create(nint sessionHandle, nint* clientHandle)
    {
        try
        {
            if (!InteropProtonApiSession.TryGetFromHandle(sessionHandle, out var session))
            {
                return -1;
            }

            var client = new ObservabilityService(session);

            *clientHandle = GCHandle.ToIntPtr(GCHandle.Alloc(client));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "observability_service_start_new", CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int CreateAndStart(nint sessionHandle, nint* clientHandle)
    {
        try
        {
            if (!InteropProtonApiSession.TryGetFromHandle(sessionHandle, out var session))
            {
                return -1;
            }

            var client = ObservabilityService.StartNew(session);

            *clientHandle = GCHandle.ToIntPtr(GCHandle.Alloc(client));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "observability_service_start", CallConvs = [typeof(CallConvCdecl)])]
    private static int Start(nint clientHandle)
    {
        try
        {
            if (!TryGetFromHandle(clientHandle, out var service))
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
    private static int Stop(nint clientHandle)
    {
        try
        {
            if (!TryGetFromHandle(clientHandle, out var service))
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
    private static int Flush(nint clientHandle, InteropAsyncCallback callback)
    {
        try
        {
            if (!TryGetFromHandle(clientHandle, out var service))
            {
                return -1;
            }

            return callback.InvokeFor(ct => FlushAsync(service, ct));
        }
        catch
        {
            return -1;
        }
    }

    private static async ValueTask<Result<InteropArray, InteropArray>> FlushAsync(ObservabilityService service, CancellationToken cancellationToken)
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
