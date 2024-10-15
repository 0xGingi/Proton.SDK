using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Proton.Sdk.CExports;

namespace Proton.Sdk.Drive.CExports;

internal static class InteropRevisionWriter
{
    internal static bool TryGetFromHandle(nint handle, [MaybeNullWhen(false)] out RevisionWriter reader)
    {
        var gcHandle = GCHandle.FromIntPtr(handle);

        reader = gcHandle.Target as RevisionWriter;

        return reader is not null;
    }

    [UnmanagedCallersOnly(EntryPoint = "revision_writer_write_to_path", CallConvs = [typeof(CallConvCdecl)])]
    private static int Write(nint writerHandle, InteropArray targetFilePath, long lastModificationTime, InteropAsyncCallback<byte> callback)
    {
        try
        {
        if (!TryGetFromHandle(writerHandle, out var writer))
        {
            return -1;
        }

        callback.InvokeFor(ct => WriteAsync(writer, targetFilePath.Utf8ToString(), DateTimeOffset.FromUnixTimeSeconds(lastModificationTime).UtcDateTime, ct));
        return 0;
        }
        catch
        {
            return -1;
        }
    }

    private static async ValueTask<Result<byte, SdkError>> WriteAsync(RevisionWriter writer, string targetFilePath, DateTime lastModificationTime, CancellationToken cancellationToken)
    {
        try
        {
            await writer.WriteAsync(targetFilePath, lastModificationTime, cancellationToken).ConfigureAwait(false);

            return (byte)VerificationStatus.Ok;
        }
        catch (Exception ex)
        {
            return new SdkError(0, ex.Message);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "revision_writer_free", CallConvs = [typeof(CallConvCdecl)])]
    private static void Free(nint handle)
    {
        try
        {
            var gcHandle = GCHandle.FromIntPtr(handle);

            if (gcHandle.Target is not RevisionWriter revisionWriter)
            {
                return;
            }

            try
            {
                revisionWriter.Dispose();
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
}
