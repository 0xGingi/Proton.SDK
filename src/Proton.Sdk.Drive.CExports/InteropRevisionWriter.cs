using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Proton.Sdk.CExports;

namespace Proton.Sdk.Drive.CExports;

internal static class InteropRevisionWriter
{
    private static bool TryGetFromHandle(nint handle, [MaybeNullWhen(false)] out RevisionWriter reader)
    {
        var gcHandle = GCHandle.FromIntPtr(handle);

        reader = gcHandle.Target as RevisionWriter;

        return reader is not null;
    }

    [UnmanagedCallersOnly(EntryPoint = "revision_writer_write_to_path", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeWrite(nint writerHandle, InteropArray revisionWriteRequestBytes, InteropAsyncCallback callback)
    {
        try
        {
            if (!TryGetFromHandle(writerHandle, out var writer))
            {
                return -1;
            }

            callback.InvokeFor(ct => InteropWriteAsync(writer, revisionWriteRequestBytes, ct));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    private static async ValueTask<Result<InteropArray, InteropArray>> InteropWriteAsync(RevisionWriter writer, InteropArray revisionWriteRequestBytes, CancellationToken cancellationToken)
    {
        try
        {
            var revisionWriteRequest = RevisionWriteRequest.Parser.ParseFrom(revisionWriteRequestBytes.ToArray());
            var lastModificationTime = DateTimeOffset.FromUnixTimeSeconds(revisionWriteRequest.LastModificationDate).DateTime;

            await writer.WriteAsync(revisionWriteRequest.TargetFilePath, lastModificationTime, _ => { }, cancellationToken).ConfigureAwait(false);

            return ResultExtensions.Success(new VerificationStatusResponse { VerificationStatus = VerificationStatus.Ok });
        }
        catch (Exception e)
        {
            return ResultExtensions.Failure(-5, e.Message);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "revision_writer_free", CallConvs = [typeof(CallConvCdecl)])]
    private static void NativeFree(nint handle)
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
