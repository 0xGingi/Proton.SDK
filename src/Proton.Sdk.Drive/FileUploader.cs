namespace Proton.Sdk.Drive;

public sealed class FileUploader
{
    private readonly ProtonDriveClient _client;
    private readonly long _expectedNumberOfBlocks;

    internal FileUploader(ProtonDriveClient client, long expectedNumberOfBlocks)
    {
        _client = client;
        _expectedNumberOfBlocks = expectedNumberOfBlocks;
    }

    public async Task<FileNode> UploadAsync(
        ShareMetadata shareMetadata,
        NodeIdentity parentFolderIdentity,
        string name,
        string mediaType,
        Stream contentInputStream,
        IEnumerable<FileSample> samples,
        DateTimeOffset? lastModificationTime,
        Action<long> onProgress,
        CancellationToken cancellationToken)
    {
        parentFolderIdentity = new NodeIdentity(shareMetadata.ShareId, parentFolderIdentity);

        var fileCreationRequest = new FileCreationRequest
        {
            ParentFolderIdentity = parentFolderIdentity,
            ShareMetadata = shareMetadata,
            MimeType = mediaType,
            Name = name,
        };
        var fileCreationResponse = await FileNode.CreateFileAsync(_client, fileCreationRequest, cancellationToken).ConfigureAwait(false);

        var fileWriteRequest = new FileWriteRequest
        {
            NodeIdentity = parentFolderIdentity,
            ShareMetadata = shareMetadata,
            RevisionMetadata = fileCreationResponse.Revision.Metadata(),
        };

        using var revisionWriter = await Revision.OpenForWritingAsync(_client, fileWriteRequest, onProgress, cancellationToken).ConfigureAwait(false);

        await revisionWriter.WriteAsync(contentInputStream, samples, lastModificationTime, cancellationToken).ConfigureAwait(false);

        return fileCreationResponse.File;
    }
}
