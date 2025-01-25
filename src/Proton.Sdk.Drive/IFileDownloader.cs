namespace Proton.Sdk.Drive;

public interface IFileDownloader : IDisposable
{
    public Task<VerificationStatus> DownloadAsync(
        INodeIdentity fileIdentity,
        IRevisionForTransfer revision,
        Stream contentOutputStream,
        Action<long, long> onProgress,
        CancellationToken cancellationToken);

    public Task<VerificationStatus> DownloadAsync(
        INodeIdentity fileIdentity,
        IRevisionForTransfer? revision,
        string targetFilePath,
        Action<long, long> onProgress,
        CancellationToken cancellationToken,
        byte[]? operationId = null);
}
