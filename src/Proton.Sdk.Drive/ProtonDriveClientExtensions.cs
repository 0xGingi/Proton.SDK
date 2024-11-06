namespace Proton.Sdk.Drive;

public static class ProtonDriveClientExtensions
{
    public static IAsyncEnumerable<INode> GetFolderChildrenAsync(
        this ProtonDriveClient client,
        NodeIdentity folderIdentity,
        CancellationToken cancellationToken,
        bool includeHidden = false)
    {
        return client.GetFolderChildrenAsync(folderIdentity, cancellationToken, includeHidden);
    }
}
