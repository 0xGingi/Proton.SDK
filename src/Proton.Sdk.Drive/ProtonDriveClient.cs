using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Drive.Devices;
using Proton.Sdk.Drive.Files;
using Proton.Sdk.Drive.Folders;
using Proton.Sdk.Drive.Links;
using Proton.Sdk.Drive.Shares;
using Proton.Sdk.Drive.Storage;
using Proton.Sdk.Drive.Verification;
using Proton.Sdk.Drive.Volumes;

namespace Proton.Sdk.Drive;

public sealed class ProtonDriveClient
{
    private readonly HttpClient _httpClient;

    public ProtonDriveClient(ProtonApiSession session)
    {
        _httpClient = session.GetHttpClient(ProtonDriveDefaults.DriveBaseRoute);

        Account = new ProtonAccountClient(session);
        SecretsCache = session.SecretsCache;

        var maxDegreeOfParallelism = Math.Max(Math.Min(Environment.ProcessorCount / 2, 10), 1);
        BlockUploader = new BlockUploader(this, maxDegreeOfParallelism);
        BlockDownloader = new BlockDownloader(this, maxDegreeOfParallelism);

        Logger = session.LoggerFactory.CreateLogger<ProtonDriveClient>();

        Logger.Log(LogLevel.Information, "ProtonDriveClient instance was created. maxDegreeOfParallelism = {maxDegreeOfParallelism}", maxDegreeOfParallelism);
    }

    internal static RecyclableMemoryStreamManager MemoryStreamManager { get; } = new();

    internal ProtonAccountClient Account { get; }
    internal ISecretsCache SecretsCache { get; }

    internal VolumesApiClient VolumesApi => new(_httpClient);
    internal DevicesApiClient DevicesApi => new(_httpClient);
    internal SharesApiClient SharesApi => new(_httpClient);
    internal LinksApiClient LinksApi => new(_httpClient);
    internal FoldersApiClient FoldersApi => new(_httpClient);
    internal FilesApiClient FilesApi => new(_httpClient);
    internal StorageApiClient StorageApi => new(_httpClient);
    internal RevisionVerificationApiClient RevisionVerificationApi => new(_httpClient);

    internal BlockUploader BlockUploader { get; }
    internal BlockDownloader BlockDownloader { get; }
    internal ILogger<ProtonDriveClient> Logger { get; }

    public Task<Volume[]> GetVolumesAsync(CancellationToken cancellationToken)
    {
        return Volume.GetAllAsync(VolumesApi, cancellationToken);
    }

    public async Task<Volume> CreateVolumeAsync(CancellationToken cancellationToken)
    {
        return await Volume.CreateAsync(this, cancellationToken).ConfigureAwait(false);
    }

    public Task<Share> GetShareAsync(ShareId shareId, CancellationToken cancellationToken)
    {
        return Share.GetAsync(this, shareId, cancellationToken);
    }

    public Task DeleteFromTrashAsync(ShareId shareId, IEnumerable<LinkId> nodeIds, CancellationToken cancellationToken)
    {
        return Share.DeleteFromTrashAsync(SharesApi, shareId, nodeIds, cancellationToken);
    }

    public Task<Node> GetNodeAsync(ShareId shareId, LinkId nodeId, CancellationToken cancellationToken)
    {
        return Node.GetAsync(this, shareId, nodeId, cancellationToken);
    }

    public IAsyncEnumerable<Node> GetFolderChildrenAsync(
        ShareId shareId,
        VolumeId volumeId,
        LinkId folderId,
        CancellationToken cancellationToken,
        bool includeHidden = false)
    {
        return FolderNode.GetChildrenAsync(this, shareId, volumeId, folderId, includeHidden, cancellationToken);
    }

    public Task<(FileNode File, Revision DraftRevision)> CreateFileAsync(
        IShareForCommand share,
        INodeIdentity parentFolder,
        string name,
        string mediaType,
        CancellationToken cancellationToken)
    {
        return FileNode.CreateAsync(this, share, parentFolder, name, mediaType, cancellationToken);
    }

    public Task<Revision[]> GetFileRevisionsAsync(ShareId shareId, INodeIdentity file, CancellationToken cancellationToken)
    {
        return FileNode.GetRevisionsAsync(this, shareId, file, cancellationToken);
    }

    public Task<Revision> GetFileRevisionAsync(ShareId shareId, INodeIdentity file, RevisionId revisionId, CancellationToken cancellationToken)
    {
        return FileNode.GetRevisionAsync(this, shareId, file, revisionId, cancellationToken);
    }

    public Task DeleteRevisionAsync(IRevisionShareBasedIdentity revision, CancellationToken cancellationToken)
    {
        return Revision.DeleteAsync(FilesApi, revision.ShareId, revision.FileId, revision.Id, cancellationToken);
    }

    public Task DeleteRevisionAsync(ShareId shareId, INodeIdentity file, RevisionId revisionId, CancellationToken cancellationToken)
    {
        return Revision.DeleteAsync(FilesApi, shareId, file.Id, revisionId, cancellationToken);
    }

    public Task<FolderNode> CreateFolderAsync(IShareForCommand share, INodeIdentity parentFolder, string name, CancellationToken cancellationToken)
    {
        return FolderNode.CreateAsync(this, share, parentFolder, name, cancellationToken);
    }

    public Task<RevisionReader> OpenRevisionForReadingAsync(
        ShareId shareId,
        INodeIdentity file,
        IRevisionForTransfer revision,
        CancellationToken cancellationToken)
    {
        return Revision.OpenForReadingAsync(this, shareId, file, revision, cancellationToken);
    }

    public Task<RevisionWriter> OpenRevisionForWritingAsync(
        IShareForCommand share,
        INodeIdentity file,
        IRevisionForTransfer revision,
        CancellationToken cancellationToken)
    {
        return Revision.OpenForWritingAsync(this, share, file, revision, cancellationToken);
    }

    public Task TrashNodesAsync(ShareId shareId, INodeIdentity folder, IEnumerable<LinkId> nodeIds, CancellationToken cancellationToken)
    {
        return FolderNode.TrashChildrenAsync(FoldersApi, shareId, folder, nodeIds, cancellationToken);
    }

    public Task DeleteNodesAsync(ShareId shareId, INodeIdentity folder, IEnumerable<LinkId> nodeIds, CancellationToken cancellationToken)
    {
        return FolderNode.DeleteChildrenAsync(FoldersApi, shareId, folder, nodeIds, cancellationToken);
    }

    public Task MoveNodeAsync(
        ShareId shareId,
        INodeForMove node,
        LinkId parentFolderId,
        IShareForCommand destinationShare,
        INodeIdentity destinationFolder,
        string nameAtDestination,
        CancellationToken cancellationToken)
    {
        return Node.MoveAsync(this, shareId, node, parentFolderId, destinationShare, destinationFolder, nameAtDestination, cancellationToken);
    }

    public Task RenameNodeAsync(
        IShareForCommand share,
        INodeForRename node,
        LinkId parentFolderId,
        string newName,
        string newMediaType,
        CancellationToken cancellationToken)
    {
        return Node.RenameAsync(this, share, node, parentFolderId, newName, newMediaType, cancellationToken);
    }
}
