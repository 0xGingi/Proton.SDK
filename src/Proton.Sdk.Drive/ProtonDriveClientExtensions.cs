namespace Proton.Sdk.Drive;

public static class ProtonDriveClientExtensions
{
    public static IAsyncEnumerable<Node> GetFolderChildrenAsync(
        this ProtonDriveClient client,
        ShareId shareId,
        INodeIdentity folder,
        CancellationToken cancellationToken,
        bool includeHidden = false)
    {
        return client.GetFolderChildrenAsync(shareId, folder.VolumeId, folder.Id, cancellationToken, includeHidden);
    }
}
