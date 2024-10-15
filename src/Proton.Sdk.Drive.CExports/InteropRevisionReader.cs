using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Proton.Sdk.CExports;

namespace Proton.Sdk.Drive.CExports;

internal static class InteropRevisionReader
{
    internal static bool TryGetFromHandle(nint handle, [MaybeNullWhen(false)] out RevisionReader reader)
    {
        var gcHandle = GCHandle.FromIntPtr(handle);

        reader = gcHandle.Target as RevisionReader;

        return reader is not null;
    }

    [UnmanagedCallersOnly(EntryPoint = "revision_reader_read", CallConvs = [typeof(CallConvCdecl)])]
    private static int Read(nint readerHandle, InteropExternalWriter externalWriter, InteropAsyncCallback<byte> callback)
    {
        try
        {
            if (!TryGetFromHandle(readerHandle, out var reader))
            {
                return -1;
            }

            callback.InvokeFor(ct => ReadAsync(reader, externalWriter.ToStream(), ct));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "revision_reader_read_to_path", CallConvs = [typeof(CallConvCdecl)])]
    private static int Read(nint readerHandle, InteropArray targetFilePath, InteropAsyncCallback<byte> callback)
    {
        try
        {
            if (!TryGetFromHandle(readerHandle, out var reader))
            {
                return -1;
            }

            callback.InvokeFor(ct => ReadAsync(reader, targetFilePath.Utf8ToString(), ct));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "revision_reader_free", CallConvs = [typeof(CallConvCdecl)])]
    private static void Free(nint handle)
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

    private static async ValueTask<Result<byte, SdkError>> ReadAsync(RevisionReader reader, Stream outputStream, CancellationToken cancellationToken)
    {
        try
        {
            var verificationStatus = await reader.ReadAsync(outputStream, cancellationToken).ConfigureAwait(false);

            return (byte)verificationStatus;
        }
        catch (Exception ex)
        {
            return new SdkError(0, ex.Message);
        }
    }

    private static async ValueTask<Result<byte, SdkError>> ReadAsync(RevisionReader reader, string targetFilePath, CancellationToken cancellationToken)
    {
        try
        {
            var verificationStatus = await reader.ReadAsync(targetFilePath, cancellationToken).ConfigureAwait(false);

            return (byte)verificationStatus;
        }
        catch (Exception ex)
        {
            return new SdkError(0, ex.Message);
        }
    }
}
