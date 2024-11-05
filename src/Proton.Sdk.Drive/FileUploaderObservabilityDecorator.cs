using Proton.Sdk.Drive.Instrumentation;

namespace Proton.Sdk.Drive;

public sealed class FileUploaderObservabilityDecorator : IFileUploader
{
    private readonly FileUploader _decoratedInstance;
    private readonly UploadAttemptRetryMonitor _uploadAttemptRetryMonitor;

    internal FileUploaderObservabilityDecorator(FileUploader decoratedInstance, UploadAttemptRetryMonitor uploadAttemptRetryMonitor)
    {
        _decoratedInstance = decoratedInstance;
        _uploadAttemptRetryMonitor = uploadAttemptRetryMonitor;
    }

    public async Task<FileNode> UploadNewFileOrRevisionAsync(
        ShareMetadata shareMetadata,
        NodeIdentity parentFolderIdentity,
        string name,
        string mediaType,
        Stream contentInputStream,
        IEnumerable<FileSample> samples,
        DateTimeOffset? lastModificationTime,
        Action<long, long> onProgress,
        CancellationToken cancellationToken)
    {
        try
        {
            var file = await _decoratedInstance.UploadNewFileOrRevisionAsync(
                shareMetadata,
                parentFolderIdentity,
                name,
                mediaType,
                contentInputStream,
                samples,
                lastModificationTime,
                onProgress,
                cancellationToken).ConfigureAwait(false);

            _uploadAttemptRetryMonitor.IncrementSuccess(parentFolderIdentity.VolumeId, parentFolderIdentity.NodeId, name);

            return file;
        }
        catch (ProtonApiException ex)
        {
            if (ex.Code is not (ResponseCode.InsufficientQuota
                or ResponseCode.InsufficientSpace
                or ResponseCode.MaxFileSizeForFreeUser
                or ResponseCode.TooManyChildren))
            {
                _uploadAttemptRetryMonitor.IncrementFailure(parentFolderIdentity.VolumeId, parentFolderIdentity.NodeId, name);
            }

            throw;
        }
    }

    public void Dispose()
    {
        _decoratedInstance.Dispose();
    }
}
