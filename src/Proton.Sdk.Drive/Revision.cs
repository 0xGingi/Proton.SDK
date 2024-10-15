using Proton.Sdk.Drive.Files;

namespace Proton.Sdk.Drive;

public sealed class Revision : IRevisionForTransfer
{
    internal Revision(VolumeId volumeId, LinkId fileId, RevisionDto properties, ExtendedAttributes extendedAttributes)
        : this(volumeId, fileId, new RevisionId(properties.Id), properties.State, properties.Size, extendedAttributes, GetSamplesSha256Digests(properties))
    {
        ManifestSignature = properties.ManifestSignature;
        SignatureEmailAddress = properties.SignatureEmailAddress;
        CreationTime = properties.CreationTime;

        SamplesSha256Digests = GetSamplesSha256Digests(properties);
    }

    internal Revision(VolumeId volumeId, LinkId fileId, RevisionId id, RevisionState state, long size, in ExtendedAttributes extendedAttributes)
        : this(volumeId, fileId, id, state, size, extendedAttributes, [])
    {
    }

    private Revision(
        VolumeId volumeId,
        LinkId fileId,
        RevisionId id,
        RevisionState state,
        long size,
        in ExtendedAttributes extendedAttributes,
        IReadOnlyList<ReadOnlyMemory<byte>> previewImageSha256Digests)
    {
        VolumeId = volumeId;
        FileId = fileId;
        Id = id;
        State = state;
        Size = extendedAttributes.Common?.Size;
        QuotaConsumption = size;
        SamplesSha256Digests = previewImageSha256Digests;
    }

    public VolumeId VolumeId { get; }
    public LinkId FileId { get; }
    public RevisionId Id { get; }
    public RevisionState State { get; }
    public long? Size { get; }
    public long QuotaConsumption { get; }
    public DateTime CreationTime { get; }

    public ReadOnlyMemory<byte>? ManifestSignature { get; }
    public string? SignatureEmailAddress { get; }
    public IReadOnlyList<ReadOnlyMemory<byte>> SamplesSha256Digests { get; }

    internal static async Task<RevisionReader> OpenForReadingAsync(
        ProtonDriveClient client,
        ShareId shareId,
        INodeIdentity file,
        IRevisionForTransfer revision,
        CancellationToken cancellationToken)
    {
        if (revision.State is RevisionState.Draft)
        {
            throw new InvalidOperationException("Draft revision cannot be opened for reading");
        }

        var contentKey = await FileNode.GetContentKeyAsync(client, shareId, file.VolumeId, file.Id, cancellationToken).ConfigureAwait(false);

        await client.BlockDownloader.FileSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        return new RevisionReader(client, shareId, file, revision, contentKey);
    }

    internal static async Task<RevisionWriter> OpenForWritingAsync(
        ProtonDriveClient client,
        IShareForCommand share,
        INodeIdentity file,
        IRevisionForTransfer revision,
        CancellationToken cancellationToken)
    {
        if (revision.State is not RevisionState.Draft)
        {
            throw new InvalidOperationException("Non-draft revision cannot be opened for writing");
        }

        var fileKey = await Node.GetKeyAsync(client, share.Id, file.VolumeId, file.Id, cancellationToken).ConfigureAwait(false);
        var contentKey = await FileNode.GetContentKeyAsync(client, share.Id, file.VolumeId, file.Id, cancellationToken).ConfigureAwait(false);
        var signingKey = await client.Account.GetAddressPrimaryKeyAsync(share.MembershipAddressId, cancellationToken).ConfigureAwait(false);

        await client.BlockUploader.FileSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        const int targetBlockSize = RevisionWriter.DefaultBlockSize;
        return new RevisionWriter(client, share, file.Id, revision.Id, fileKey, contentKey, signingKey, targetBlockSize, targetBlockSize * 3 / 2);
    }

    internal static async Task DeleteAsync(FilesApiClient filesApi, ShareId shareId, LinkId fileId, RevisionId revisionId, CancellationToken cancellationToken)
    {
        await filesApi.DeleteRevisionAsync(shareId, fileId, revisionId, cancellationToken).ConfigureAwait(false);
    }

    private static ReadOnlyMemory<byte>[] GetSamplesSha256Digests(RevisionDto properties)
    {
        if (properties.Thumbnails is null)
        {
            return [];
        }

        Span<ThumbnailType> keys = stackalloc ThumbnailType[properties.Thumbnails.Count];
        var digests = new ReadOnlyMemory<byte>[properties.Thumbnails.Count];

        for (var i = 0; i < properties.Thumbnails.Count; ++i)
        {
            var thumbnail = properties.Thumbnails[i];
            keys[i] = thumbnail.Type;
            digests[i] = thumbnail.HashDigest;
        }

        keys.Sort(digests.AsSpan());

        return digests;
    }
}
