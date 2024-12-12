namespace Proton.Sdk.Drive;

public sealed class FileDownloader : IDisposable
{
    private readonly ProtonDriveClient _client;
    private volatile int _remainingNumberOfBlocksToList;

    internal FileDownloader(ProtonDriveClient client, int expectedNumberOfBlocks)
    {
        _client = client;
        _remainingNumberOfBlocksToList = expectedNumberOfBlocks;
    }

    public async Task<VerificationStatus> DownloadAsync(
        INodeIdentity fileIdentity,
        IRevisionForTransfer revision,
        Stream contentOutputStream,
        Action<long, long> onProgress,
        CancellationToken cancellationToken)
    {
        using var revisionReader = await Revision.OpenForReadingAsync(_client, fileIdentity, revision, cancellationToken, ReleaseBlockListing)
            .ConfigureAwait(false);

        return await revisionReader.ReadAsync(contentOutputStream, onProgress, cancellationToken).ConfigureAwait(false);
    }

    public async Task<VerificationStatus> DownloadAsync(
        INodeIdentity fileIdentity,
        IRevisionForTransfer revision,
        string targetFilePath,
        Action<long, long> onProgress,
        CancellationToken cancellationToken,
        byte[]? operationId = null)
    {
        using var revisionReader = await Revision
            .OpenForReadingAsync(_client, fileIdentity, revision, cancellationToken, ReleaseBlockListing, operationId).ConfigureAwait(false);

        var fileStream = File.Open(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);

        await using (fileStream.ConfigureAwait(false))
        {
            return await revisionReader.ReadAsync(fileStream, onProgress, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        if (_remainingNumberOfBlocksToList <= 0)
        {
            return;
        }

        _client.BlockListingSemaphore.Release(_remainingNumberOfBlocksToList);
        _remainingNumberOfBlocksToList = 0;
    }

    private void ReleaseBlockListing(int numberOfBlockListings)
    {
        var newRemainingNumberOfBlocks = Interlocked.Add(ref _remainingNumberOfBlocksToList, -numberOfBlockListings);

        var amountToRelease = Math.Max(newRemainingNumberOfBlocks >= 0 ? numberOfBlockListings : newRemainingNumberOfBlocks + numberOfBlockListings, 0);

        _client.BlockListingSemaphore.Release(amountToRelease);
    }
}
