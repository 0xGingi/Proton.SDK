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

    /// <summary>
    /// Creates a new instance of <see cref="ProtonDriveClient"/>.
    /// </summary>
    /// <param name="session">Authentication session</param>
    /// <param name="id">Unique identifier for this client used to identify draft revisions that it may re-use.</param>
    public ProtonDriveClient(ProtonApiSession session, string? id = default)
    {
        _httpClient = session.GetHttpClient(ProtonDriveDefaults.DriveBaseRoute);

        Id = id ?? Guid.NewGuid().ToString();

        Account = new ProtonAccountClient(session);
        SecretsCache = session.SecretsCache;

        var maxDegreeOfBlockTransferParallelism = Math.Max(Math.Min(Environment.ProcessorCount / 2, 8), 2);
        var maxDegreeOfBlockProcessingParallelism = maxDegreeOfBlockTransferParallelism + Math.Min(Math.Max(maxDegreeOfBlockTransferParallelism / 2, 2), 4);
        RevisionBlockListingSemaphore = new FifoFlexibleSemaphore(maxDegreeOfBlockProcessingParallelism);
        RevisionCreationSemaphore = new FifoFlexibleSemaphore(maxDegreeOfBlockProcessingParallelism);
        BlockUploader = new BlockUploader(this, maxDegreeOfBlockTransferParallelism);
        BlockDownloader = new BlockDownloader(this, maxDegreeOfBlockTransferParallelism);

        Logger = session.LoggerFactory.CreateLogger<ProtonDriveClient>();
    }

    public string Id { get; }

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
    internal FifoFlexibleSemaphore RevisionBlockListingSemaphore { get; }
    internal FifoFlexibleSemaphore RevisionCreationSemaphore { get; }
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

    public async Task<FileUploader> WaitForFileUploaderAsync(long size, int numberOfSamples, CancellationToken cancellationToken)
    {
        var expectedNumberOfBlocks = (int)size.DivideAndRoundUp(RevisionWriter.DefaultBlockSize) + numberOfSamples;

        await RevisionCreationSemaphore.EnterAsync(expectedNumberOfBlocks, cancellationToken).ConfigureAwait(false);

        return new FileUploader(this, expectedNumberOfBlocks);
    }

    public async Task<FileDownloader> WaitForFileDownloaderAsync(CancellationToken cancellationToken)
    {
        await RevisionBlockListingSemaphore.EnterAsync(1, cancellationToken).ConfigureAwait(false);

        return new FileDownloader(this);
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
