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
        Action<long, long> onProgress,
        CancellationToken cancellationToken)
    {
        try
        {
            using var revisionReader = await Revision.OpenForReadingAsync(_client, fileIdentity, revision, cancellationToken).ConfigureAwait(false);

            return await revisionReader.ReadAsync(contentOutputStream, onProgress, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _client.RevisionBlockListingSemaphore.Release(1);
        }
    }

    public async Task<VerificationStatus> DownloadAsync(
        INodeIdentity fileIdentity,
        IRevisionForTransfer revision,
        string targetFilePath,
        Action<long, long> onProgress,
        CancellationToken cancellationToken,
        byte[]? operationId = null)
    {
        try
        {
            using var revisionReader = await Revision.OpenForReadingAsync(_client, fileIdentity, revision, cancellationToken, operationId).ConfigureAwait(false);

            var fileStream = File.Open(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);

            await using (fileStream.ConfigureAwait(false))
            {
                return await revisionReader.ReadAsync(fileStream, onProgress, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _client.RevisionBlockListingSemaphore.Release(1);
        }
    }
}
