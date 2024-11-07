using Proton.Sdk.Drive.Files;

namespace Proton.Sdk.Drive;

public sealed class FileUploader : IDisposable
{
    private readonly ProtonDriveClient _client;
    private volatile int _remainingNumberOfBlocks;

    internal FileUploader(ProtonDriveClient client, int expectedNumberOfBlocks)
    {
        _client = client;
        _remainingNumberOfBlocks = expectedNumberOfBlocks;
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
        parentFolderIdentity = new NodeIdentity(shareMetadata.ShareId, parentFolderIdentity);
        var fileUploadRequest = new FileUploadRequest
        {
            ShareMetadata = shareMetadata,
            ParentFolderIdentity = parentFolderIdentity,
            Name = name,
            MimeType = mediaType,
        };

        FileUploadResponse fileUploadResponse;
        try
        {
            fileUploadResponse = await FileNode.CreateFileAsync(_client, fileUploadRequest, cancellationToken).ConfigureAwait(false);
        }
        catch (ProtonApiException<RevisionConflictResponse> ex) when (ex.Response is { Conflict: { RevisionId: not null, DraftRevisionId: null } })
        {
            var conflictingNode = await Node.GetAsync(_client, shareMetadata.ShareId, new LinkId(ex.Response.Conflict.LinkId), cancellationToken).ConfigureAwait(false);
            if (conflictingNode is not FileNode conflictingFile)
            {
                throw;
            }

            fileUploadResponse = new FileUploadResponse
            {
                File = conflictingFile,
                Revision = await Revision.CreateAsync(_client, shareMetadata, conflictingFile.NodeIdentity, new RevisionId(ex.Response.Conflict.RevisionId), cancellationToken)
                    .ConfigureAwait(false)
            };
        }

        await UploadAsync(shareMetadata, fileUploadResponse.File.NodeIdentity, fileUploadResponse.Revision, contentInputStream, samples, lastModificationTime, onProgress, cancellationToken).ConfigureAwait(false);

        return fileUploadResponse.File;
    }

    public async Task<FileNode> UploadNewFileAsync(
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
        parentFolderIdentity = new NodeIdentity(shareMetadata.ShareId, parentFolderIdentity);

        var fileCreationRequest = new FileUploadRequest
        {
            ParentFolderIdentity = parentFolderIdentity,
            ShareMetadata = shareMetadata,
            MimeType = mediaType,
            Name = name,
        };
        var fileCreationResponse = await FileNode.CreateFileAsync(_client, fileCreationRequest, cancellationToken).ConfigureAwait(false);

        await UploadAsync(shareMetadata, parentFolderIdentity, fileCreationResponse.Revision, contentInputStream, samples, lastModificationTime, onProgress, cancellationToken).ConfigureAwait(false);

        return fileCreationResponse.File;
    }

    public async Task<Revision> UploadNewRevisionAsync(
        ShareMetadata shareMetadata,
        NodeIdentity fileIdentity,
        RevisionId lastKnownRevisionId,
        Stream contentInputStream,
        IEnumerable<FileSample> samples,
        DateTimeOffset? lastModificationTime,
        Action<long, long> onProgress,
        CancellationToken cancellationToken)
    {
        var revision = await Revision.CreateAsync(_client, shareMetadata, fileIdentity, lastKnownRevisionId, cancellationToken).ConfigureAwait(false);

        await UploadAsync(shareMetadata, fileIdentity, revision, contentInputStream, samples, lastModificationTime, onProgress, cancellationToken).ConfigureAwait(false);

        return revision;
    }

    public void Dispose()
    {
        _client.RevisionCreationSemaphore.Release(_remainingNumberOfBlocks);
        _remainingNumberOfBlocks = 0;
    }

    private async ValueTask UploadAsync(
        ShareMetadata shareMetadata,
        NodeIdentity fileIdentity,
        Revision revision,
        Stream contentInputStream,
        IEnumerable<FileSample> samples,
        DateTimeOffset? lastModificationTime,
        Action<long, long> onProgress,
        CancellationToken cancellationToken)
    {
        var fileUploadRequest = new RevisionUploadRequest
        {
            FileIdentity = fileIdentity,
            ShareMetadata = shareMetadata,
            RevisionMetadata = revision.Metadata(),
        };

        using var revisionWriter = await Revision.OpenForWritingAsync(_client, fileUploadRequest, ReleaseBlocks, cancellationToken).ConfigureAwait(false);

        await revisionWriter.WriteAsync(contentInputStream, samples, lastModificationTime, onProgress, cancellationToken).ConfigureAwait(false);
    }

    private void ReleaseBlocks(int numberOfBlocks)
    {
        _client.RevisionCreationSemaphore.Release(_remainingNumberOfBlocks);
        Interlocked.Decrement(ref _remainingNumberOfBlocks);
    }
}
