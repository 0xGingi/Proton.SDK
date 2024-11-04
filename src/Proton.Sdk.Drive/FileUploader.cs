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
        IShareForCommand share,
        INodeIdentity parentFolder,
        string name,
        string mediaType,
        Stream contentInputStream,
        IEnumerable<FileSample> samples,
        DateTimeOffset? lastModificationTime,
        CancellationToken cancellationToken)
    {
        var (file, revision) = await FileNode.CreateAsync(_client, share, parentFolder, name, mediaType, cancellationToken).ConfigureAwait(false);

        using var revisionWriter = await Revision.OpenForWritingAsync(_client, share, file, revision, cancellationToken).ConfigureAwait(false);

        await revisionWriter.WriteAsync(contentInputStream, samples, lastModificationTime, cancellationToken).ConfigureAwait(false);

        return file;
    }
}
