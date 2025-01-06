using Microsoft.Extensions.Logging;

namespace Proton.Sdk.Drive;

public interface IFileUploader : IDisposable
{
    public ILogger Logger { get; }

    public Task<FileNode> UploadNewFileOrRevisionAsync(
        ShareMetadata shareMetadata,
        NodeIdentity parentFolderIdentity,
        string name,
        string mediaType,
        Stream contentInputStream,
        IEnumerable<FileSample> samples,
        DateTimeOffset? lastModificationTime,
        Action<long, long> onProgress,
        CancellationToken cancellationToken,
        byte[]? operationId = null);

    public Task<FileUploadResponse> UploadNewFileAsync(
        ShareMetadata shareMetadata,
        NodeIdentity parentFolderIdentity,
        string name,
        string mediaType,
        Stream contentInputStream,
        IEnumerable<FileSample> samples,
        DateTimeOffset? lastModificationTime,
        Action<long, long> onProgress,
        CancellationToken cancellationToken,
        byte[]? operationId = null);

    public Task<Revision> UploadNewRevisionAsync(
        ShareMetadata shareMetadata,
        NodeIdentity fileIdentity,
        RevisionId? lastKnownRevisionId,
        Stream contentInputStream,
        IEnumerable<FileSample> samples,
        DateTimeOffset? lastModificationTime,
        Action<long, long> onProgress,
        CancellationToken cancellationToken,
        byte[]? operationId = null);
}
