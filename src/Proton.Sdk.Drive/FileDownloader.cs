namespace Proton.Sdk.Drive;

public sealed class FileDownloader
{
    private readonly ProtonDriveClient _client;

    internal FileDownloader(ProtonDriveClient client)
    {
        _client = client;
    }

    public async Task<VerificationStatus> DownloadAsync(
        INodeIdentity fileIdentity,
        IRevisionForTransfer revision,
        Stream contentOutputStream,
        CancellationToken cancellationToken)
    {
        try
        {
            using var revisionReader = await Revision.OpenForReadingAsync(_client, fileIdentity, revision, cancellationToken).ConfigureAwait(false);

            return await revisionReader.ReadAsync(contentOutputStream, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _client.RevisionBlockListingSemaphore.Release(1);
        }
    }
}
