using Proton.Sdk.Drive.Files;

namespace Proton.Sdk.Drive;

public sealed class FileUploader : IFileUploader
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
        CancellationToken cancellationToken,
        byte[]? operationId = null)
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
            fileUploadResponse = await FileNode.CreateFileAsync(_client, fileUploadRequest, cancellationToken, operationId).ConfigureAwait(false);
        }
        catch (ProtonApiException<RevisionConflictResponse> ex) when (ex.Response is { Conflict: { RevisionId: not null, DraftRevisionId: null } })
        {
            var conflictingNode = await Node.GetAsync(
                _client,
                shareMetadata.ShareId,
                new LinkId(ex.Response.Conflict.LinkId),
                cancellationToken,
                operationId).ConfigureAwait(false);

            if (conflictingNode is not FileNode conflictingFile)
            {
                throw;
            }

            fileUploadResponse = new FileUploadResponse
            {
                File = conflictingFile,
                Revision = await Revision.CreateAsync(
                    _client,
                    shareMetadata,
                    conflictingFile.NodeIdentity,
                    new RevisionId(ex.Response.Conflict.RevisionId),
                    cancellationToken,
                    operationId).ConfigureAwait(false),
            };
        }

        await UploadAsync(
            shareMetadata,
            fileUploadResponse.File.NodeIdentity,
            fileUploadResponse.Revision,
            contentInputStream,
            samples,
            lastModificationTime,
            onProgress,
            cancellationToken,
            operationId).ConfigureAwait(false);

        return fileUploadResponse.File;
    }

    public async Task<FileUploadResponse> UploadNewFileAsync(
        ShareMetadata shareMetadata,
        NodeIdentity parentFolderIdentity,
        string name,
        string mediaType,
        Stream contentInputStream,
        IEnumerable<FileSample> samples,
        DateTimeOffset? lastModificationTime,
        Action<long, long> onProgress,
        CancellationToken cancellationToken,
        byte[]? operationId = null)
    {
        parentFolderIdentity = new NodeIdentity(shareMetadata.ShareId, parentFolderIdentity);

        var fileCreationRequest = new FileUploadRequest
        {
            ParentFolderIdentity = parentFolderIdentity,
            ShareMetadata = shareMetadata,
            MimeType = mediaType,
            Name = name,
        };

        var fileCreationResponse = await FileNode.CreateFileAsync(_client, fileCreationRequest, cancellationToken, operationId).ConfigureAwait(false);

        await UploadAsync(
            shareMetadata,
            fileCreationResponse.File.NodeIdentity,
            fileCreationResponse.Revision,
            contentInputStream,
            samples,
            lastModificationTime,
            onProgress,
            cancellationToken,
            operationId).ConfigureAwait(false);

        return new FileUploadResponse
        {
            File = fileCreationResponse.File,
            Revision = fileCreationResponse.Revision,
        };
    }

    public async Task<Revision> UploadNewRevisionAsync(
        ShareMetadata shareMetadata,
        NodeIdentity fileIdentity,
        RevisionId lastKnownRevisionId,
        Stream contentInputStream,
        IEnumerable<FileSample> samples,
        DateTimeOffset? lastModificationTime,
        Action<long, long> onProgress,
        CancellationToken cancellationToken,
        byte[]? operationId = null)
    {
        var revision = await Revision.CreateAsync(
            _client,
            shareMetadata,
            fileIdentity,
            lastKnownRevisionId,
            cancellationToken,
            operationId).ConfigureAwait(false);

        await UploadAsync(
            shareMetadata,
            fileIdentity,
            revision,
            contentInputStream,
            samples,
            lastModificationTime,
            onProgress,
            cancellationToken,
            operationId).ConfigureAwait(false);

        return revision;
    }

    public void Dispose()
    {
        if (_remainingNumberOfBlocks <= 0)
        {
            return;
        }

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
        CancellationToken cancellationToken,
        byte[]? operationId = null)
    {
        var fileUploadRequest = new RevisionUploadRequest
        {
            FileIdentity = fileIdentity,
            ShareMetadata = shareMetadata,
            RevisionMetadata = revision.Metadata(),
        };

        using var revisionWriter = await Revision.OpenForWritingAsync(_client, fileUploadRequest, ReleaseBlocks, cancellationToken, operationId).ConfigureAwait(false);

        await revisionWriter.WriteAsync(contentInputStream, samples, lastModificationTime, onProgress, cancellationToken, operationId).ConfigureAwait(false);
    }

    private void ReleaseBlocks(int numberOfBlocks)
    {
        var newRemainingNumberOfBlocks = Interlocked.Add(ref _remainingNumberOfBlocks, -numberOfBlocks);

        var amountToRelease = Math.Max(newRemainingNumberOfBlocks >= 0 ? numberOfBlocks : newRemainingNumberOfBlocks + numberOfBlocks, 0);

        _client.RevisionCreationSemaphore.Release(amountToRelease);
    }
}
