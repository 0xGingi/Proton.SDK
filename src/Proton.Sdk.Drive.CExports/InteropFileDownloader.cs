using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Google.Protobuf;
using Proton.Sdk.CExports;

namespace Proton.Sdk.Drive.CExports;

internal static class InteropFileDownloader
{
    private static bool TryGetFromHandle(nint downloaderHandle, [MaybeNullWhen(false)] out FileDownloader downloader)
    {
        var gcHandle = GCHandle.FromIntPtr(downloaderHandle);

        downloader = gcHandle.Target as FileDownloader;

        return downloader is not null;
    }

    [UnmanagedCallersOnly(EntryPoint = "downloader_create", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeRead(nint clientHandle, InteropArray emptyRequest, InteropAsyncCallback callback)
    {
        try
        {
            if (!InteropProtonDriveClient.TryGetFromHandle(clientHandle, out var client))
            {
                return -1;
            }

            return callback.InvokeFor(ct => InteropCreateDownloader(client, ct));
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "downloader_download_file", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeDownloadFile(nint downloaderHandle, InteropArray fileDownloadRequestBytes, InteropAsyncCallbackWithProgress callback)
    {
        try
        {
            if (!TryGetFromHandle(downloaderHandle, out var downloader))
            {
                return -1;
            }

            return callback.AsyncCallback.InvokeFor(ct => InteropDownloadFileAsync(downloader, fileDownloadRequestBytes, callback.ProgressCallback, ct));
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "downloader_free", CallConvs = [typeof(CallConvCdecl)])]
    private static void NativeFree(nint downloaderHandle)
    {
        try
        {
            var gcHandle = GCHandle.FromIntPtr(downloaderHandle);

            if (gcHandle.Target is not FileDownloader fileDownloader)
            {
                return;
            }

            try
            {
                fileDownloader.Dispose();
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

    private static async ValueTask<Result<InteropArray, InteropArray>> InteropCreateDownloader(
        ProtonDriveClient client,
        CancellationToken cancellationToken)
    {
        try
        {
            var downloader = await client.WaitForFileDownloaderAsync(cancellationToken).ConfigureAwait(false);

            var handle = GCHandle.ToIntPtr(GCHandle.Alloc(downloader));
            return ResultExtensions.Success(new IntResponse { Value = handle });
        }
        catch (Exception e)
        {
            return ResultExtensions.Failure(e);
        }
    }

    private static async ValueTask<Result<InteropArray, InteropArray>> InteropDownloadFileAsync(
        FileDownloader downloader,
        InteropArray fileDownloadRequestBytes,
        InteropProgressCallback progressCallback,
        CancellationToken cancellationToken)
    {
        try
        {
            var fileDownloadRequest = FileDownloadRequest.Parser.ParseFrom(fileDownloadRequestBytes.AsReadOnlySpan());

            var verificationStatus = await downloader.DownloadAsync(
                fileDownloadRequest.FileIdentity,
                fileDownloadRequest.RevisionMetadata,
                fileDownloadRequest.TargetFilePath,
                (completed, total) => progressCallback.UpdateProgress(completed, total),
                cancellationToken,
                fileDownloadRequest.OperationId.ToByteArray()).ConfigureAwait(false);

            return ResultExtensions.Success(new VerificationStatusResponse { VerificationStatus = verificationStatus });
        }
        catch (Exception e)
        {
            return ResultExtensions.Failure(e);
        }
    }
}
