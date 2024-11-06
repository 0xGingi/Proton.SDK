using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Proton.Sdk.CExports;

namespace Proton.Sdk.Drive.CExports;

internal static class InteropRevisionReader
{
    private static bool TryGetFromHandle(nint handle, [MaybeNullWhen(false)] out RevisionReader reader)
    {
        var gcHandle = GCHandle.FromIntPtr(handle);

        reader = gcHandle.Target as RevisionReader;

        return reader is not null;
    }

    [UnmanagedCallersOnly(EntryPoint = "revision_reader_read", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeRead(nint readerHandle, InteropExternalWriter externalWriter, InteropAsyncCallback callback)
    {
        try
        {
            if (!TryGetFromHandle(readerHandle, out var reader))
            {
                return -1;
            }

            callback.InvokeFor(ct => InteropReadAsync(reader, externalWriter.ToStream(), ct));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "revision_reader_read_to_path", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeRead(nint readerHandle, InteropArray revisionReadRequestBytes, InteropAsyncCallback callback)
    {
        try
        {
            if (!TryGetFromHandle(readerHandle, out var reader))
            {
                return -1;
            }

            callback.InvokeFor(ct => InteropReadAsync(reader, revisionReadRequestBytes, ct));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "revision_reader_free", CallConvs = [typeof(CallConvCdecl)])]
    private static void NativeFree(nint handle)
    {
        try
        {
            var gcHandle = GCHandle.FromIntPtr(handle);

            if (gcHandle.Target is not RevisionReader revisionReader)
            {
                return;
            }

            try
            {
                revisionReader.Dispose();
            }
            finally
            {
                gcHandle.Free();
            }
        }
        catch
        {
            // Ignore
        }
    }

    private static async ValueTask<Result<InteropArray, InteropArray>> InteropReadAsync(RevisionReader reader, Stream outputStream, CancellationToken cancellationToken)
    {
        try
        {
            var verificationStatus = await reader.ReadAsync(outputStream, cancellationToken).ConfigureAwait(false);

            return ResultExtensions.Success(new VerificationStatusResponse { VerificationStatus = verificationStatus });
        }
        catch (Exception e)
        {
            return ResultExtensions.Failure(-5, e.Message);
        }
    }

    private static async ValueTask<Result<InteropArray, InteropArray>> InteropReadAsync(RevisionReader reader, InteropArray revisionReadRequestBytes, CancellationToken cancellationToken)
    {
        try
        {
            var revisionReadRequest = RevisionWriteRequest.Parser.ParseFrom(revisionReadRequestBytes.ToArray());

            var verificationStatus = await reader.ReadAsync(revisionReadRequest.TargetFilePath, cancellationToken).ConfigureAwait(false);

            return ResultExtensions.Success(new VerificationStatusResponse { VerificationStatus = verificationStatus });
        }
        catch (Exception e)
        {
            return ResultExtensions.Failure(-5, e.Message);
        }
    }
}
