using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Google.Protobuf;
using Proton.Sdk.CExports;

namespace Proton.Sdk.Drive.CExports;

internal static class InteropFileUploader
{
    private static bool TryGetFromHandle(nint handle, [MaybeNullWhen(false)] out IFileUploader uploader)
    {
        if (handle == 0)
        {
            uploader = null;
            return false;
        }

        var gcHandle = GCHandle.FromIntPtr(handle);

        uploader = gcHandle.Target as IFileUploader;

        return uploader is not null;
    }

    [UnmanagedCallersOnly(EntryPoint = "uploader_create", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeCreate(nint clientHandle, InteropArray fileUploaderCreationRequestBytes, InteropAsyncCallback callback)
    {
        try
        {
            if (!InteropProtonDriveClient.TryGetFromHandle(clientHandle, out var client))
            {
                return -1;
            }

            return callback.InvokeFor(ct => InteropCreateUploader(client, fileUploaderCreationRequestBytes, ct));
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "uploader_upload_file_or_revision", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeUploadFile(nint uploaderHandle, InteropArray fileUploadRequestBytes, InteropAsyncCallbackWithProgress callback)
    {
        try
        {
            if (!TryGetFromHandle(uploaderHandle, out var uploader))
            {
                return -1;
            }

            return callback.AsyncCallback.InvokeFor(ct => InteropUploadFileOrRevisionAsync(uploader, fileUploadRequestBytes, callback.ProgressCallback, ct));
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "uploader_upload_revision", CallConvs = [typeof(CallConvCdecl)])]
    private static int NativeUploadRevision(nint uploaderHandle, InteropArray revisionUploadRequestBytes, InteropAsyncCallbackWithProgress callback)
    {
        try
        {
            if (!TryGetFromHandle(uploaderHandle, out var uploader))
            {
                return -1;
            }

            return callback.AsyncCallback.InvokeFor(ct => InteropUploadRevisionAsync(uploader, revisionUploadRequestBytes, callback.ProgressCallback, ct));
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "uploader_free", CallConvs = [typeof(CallConvCdecl)])]
    private static void NativeFree(nint uploaderHandle)
    {
        try
        {
            var gcHandle = GCHandle.FromIntPtr(uploaderHandle);

            if (gcHandle.Target is not IFileUploader fileUploader)
            {
                return;
            }

            try
            {
                fileUploader.Dispose();
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

    private static async ValueTask<Result<InteropArray, InteropArray>> InteropCreateUploader(
        ProtonDriveClient client,
        InteropArray fileUploaderCreationRequestBytes,
        CancellationToken cancellationToken)
    {
        try
        {
            var fileUploaderCreationRequest = FileUploaderCreationRequest.Parser.ParseFrom(fileUploaderCreationRequestBytes.AsReadOnlySpan());

            var uploader = await client.WaitForFileUploaderAsync(
                fileUploaderCreationRequest.FileSize,
                fileUploaderCreationRequest.NumberOfSamples,
                cancellationToken).ConfigureAwait(false);

            var handle = GCHandle.ToIntPtr(GCHandle.Alloc(uploader));
            return ResultExtensions.Success(new IntResponse { Value = handle });
        }
        catch (Exception e)
        {
            return ResultExtensions.Failure(e, InteropDriveErrorConverter.SetDomainAndCodes);
        }
    }

    private static async ValueTask<Result<InteropArray, InteropArray>> InteropUploadFileOrRevisionAsync(
        IFileUploader uploader,
        InteropArray fileUploadRequestBytes,
        InteropProgressCallback progressCallback,
        CancellationToken cancellationToken)
    {
        try
        {
            var fileUploadRequest = FileUploadRequest.Parser.ParseFrom(fileUploadRequestBytes.AsReadOnlySpan());

            var samples = fileUploadRequest.HasThumbnail
                ? new[] { new FileSample(FileSampleType.Thumbnail, new ArraySegment<byte>(fileUploadRequest.Thumbnail.ToByteArray())) }
                : [];

            FileStream fileStream;
            try
            {
                fileStream = File.OpenRead(fileUploadRequest.SourceFilePath);
            }
            catch
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }

            await using (fileStream)
            {
                var response = await uploader.UploadNewFileOrRevisionAsync(
                    fileUploadRequest.ShareMetadata,
                    fileUploadRequest.ParentFolderIdentity,
                    fileUploadRequest.Name,
                    fileUploadRequest.MimeType,
                    fileStream,
                    samples,
                    DateTimeOffset.FromUnixTimeSeconds(fileUploadRequest.LastModificationDate),
                    (completed, total) => progressCallback.UpdateProgress(completed, total),
                    cancellationToken,
                    fileUploadRequest.OperationId.ToByteArray()).ConfigureAwait(false);

                return ResultExtensions.Success(response);
            }
        }
        catch (Exception e)
        {
            return ResultExtensions.Failure(e, InteropDriveErrorConverter.SetDomainAndCodes);
        }
    }

    private static async ValueTask<Result<InteropArray, InteropArray>> InteropUploadRevisionAsync(
        IFileUploader uploader,
        InteropArray revisionUploadRequestBytes,
        InteropProgressCallback progressCallback,
        CancellationToken cancellationToken)
    {
        try
        {
            var revisionUploadRequest = RevisionUploadRequest.Parser.ParseFrom(revisionUploadRequestBytes.AsReadOnlySpan());

            var samples = revisionUploadRequest.HasThumbnail
                ? new[] { new FileSample(FileSampleType.Thumbnail, new ArraySegment<byte>(revisionUploadRequest.Thumbnail.ToByteArray())) }
                : [];

            FileStream fileStream;
            try
            {
                fileStream = File.OpenRead(revisionUploadRequest.SourceFilePath);
            }
            catch
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }

            await using (fileStream)
            {
                var response = await uploader.UploadNewRevisionAsync(
                    revisionUploadRequest.ShareMetadata,
                    revisionUploadRequest.FileIdentity,
                    revisionUploadRequest.RevisionMetadata?.RevisionId,
                    fileStream,
                    samples,
                    DateTimeOffset.FromUnixTimeSeconds(revisionUploadRequest.LastModificationDate),
                    (completed, total) => progressCallback.UpdateProgress(completed, total),
                    cancellationToken,
                    revisionUploadRequest.OperationId.ToByteArray()).ConfigureAwait(false);

                return ResultExtensions.Success(response);
            }
        }
        catch (Exception e)
        {
            return ResultExtensions.Failure(e, InteropDriveErrorConverter.SetDomainAndCodes);
        }
    }
}
