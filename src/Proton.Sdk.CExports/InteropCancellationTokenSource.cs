﻿using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Proton.Sdk.CExports;

internal static class InteropCancellationTokenSource
{
    internal static bool TryGetFromHandle(nint handle, [MaybeNullWhen(false)] out CancellationTokenSource cancellationTokenSource)
    {
        var gcHandle = GCHandle.FromIntPtr(handle);

        cancellationTokenSource = gcHandle.Target as CancellationTokenSource;

        return cancellationTokenSource is not null;
    }

    internal static bool TryGetTokenFromHandle(nint handle, out CancellationToken cancellationToken)
    {
        if (handle == default)
        {
            cancellationToken = CancellationToken.None;
            return true;
        }

        if (!TryGetFromHandle(handle, out var cancellationTokenSource))
        {
            cancellationToken = default;
            return false;
        }

        cancellationToken = cancellationTokenSource.Token;
        return true;
    }

    [UnmanagedCallersOnly(EntryPoint = "cancellation_token_source_create", CallConvs = [typeof(CallConvCdecl)])]
    private static nint Create()
    {
        return GCHandle.ToIntPtr(GCHandle.Alloc(new CancellationTokenSource()));
    }

    [UnmanagedCallersOnly(EntryPoint = "cancellation_token_source_cancel", CallConvs = [typeof(CallConvCdecl)])]
    private static void Cancel(nint cancellationTokenSourceHandle)
    {
        try
        {
            if (!TryGetFromHandle(cancellationTokenSourceHandle, out var cancellationTokenSource))
            {
                return;
            }

            cancellationTokenSource.Cancel();
        }
        catch
        {
            // Ignore
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "cancellation_token_source_free", CallConvs = [typeof(CallConvCdecl)])]
    private static void Free(nint cancellationTokenSourceHandle)
    {
        try
        {
            var gcHandle = GCHandle.FromIntPtr(cancellationTokenSourceHandle);

            if (gcHandle.Target is not CancellationTokenSource cancellationTokenSource)
            {
                return;
            }

            gcHandle.Free();

            cancellationTokenSource.Dispose();
        }
        catch
        {
            // Ignore
        }
    }
}
