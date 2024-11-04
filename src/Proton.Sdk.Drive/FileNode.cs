using System.Text.Json;
using Proton.Cryptography.Pgp;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Drive.Files;
using Proton.Sdk.Drive.Serialization;

namespace Proton.Sdk.Drive;

public sealed class FileNode(VolumeId volumeId, LinkId id, LinkId? parentId, string name, ReadOnlyMemory<byte> nameHashDigest, NodeState state)
    : Node(volumeId, id, parentId, name, nameHashDigest, state)
{
    internal FileNode(
        VolumeId volumeId,
        LinkId id,
        LinkId? parentId,
        string name,
        ReadOnlyMemory<byte> nameHashDigest,
        NodeState state,
        (RevisionDto Properties, ExtendedAttributes ExtendedAttributes)? activeRevision = null)
        : this(volumeId, id, parentId, name, nameHashDigest, state)
    {
        if (activeRevision is not null)
        {
            var (activeRevisionProperties, extendedAttributes) = activeRevision.Value;
            ActiveRevision = new Revision(volumeId, id, activeRevisionProperties, extendedAttributes);
        }
    }

    public Revision? ActiveRevision { get; }

    internal static async Task<(FileNode File, Revision DraftRevision)> CreateAsync(
        ProtonDriveClient client,
        IShareForCommand share,
        INodeIdentity parentFolder,
        string name,
        string mediaType,
        CancellationToken cancellationToken)
    {
        var parentFolderKey = await GetKeyAsync(client, share.Id, parentFolder.VolumeId, parentFolder.Id, cancellationToken).ConfigureAwait(false);
        var signingKey = await client.Account.GetAddressPrimaryKeyAsync(share.MembershipAddressId, cancellationToken).ConfigureAwait(false);
        var parentFolderHashKey = await GetHashKeyAsync(client, share.Id, parentFolder.VolumeId, parentFolder.Id, cancellationToken).ConfigureAwait(false);

        GetCommonCreationParameters(
            name,
            parentFolderKey,
            parentFolderHashKey.Span,
            signingKey,
            out var key,
            out var nameSessionKey,
            out var passphraseSessionKey,
            out var encryptedName,
            out var nameHashDigest,
            out var encryptedKeyPassphrase,
            out var passphraseSignature,
            out var lockedKeyBytes);

        var contentKey = PgpSessionKey.Generate();
        var (contentKeyToken, _) = contentKey.Export();

        var parameters = new FileCreationParameters
        {
            Name = encryptedName,
            NameHashDigest = nameHashDigest,
            ParentLinkId = parentFolder.Id.Value,
            Passphrase = encryptedKeyPassphrase,
            PassphraseSignature = passphraseSignature,
            SignatureEmailAddress = share.MembershipEmailAddress,
            Key = lockedKeyBytes,
            MediaType = mediaType,
            ContentKeyPacket = key.EncryptSessionKey(contentKey),
            ContentKeyPacketSignature = key.Sign(contentKeyToken),
        };

        LinkId id;
        RevisionId revisionId;
        try
        {
            var response = await client.FilesApi.CreateFileAsync(share.Id, parameters, cancellationToken).ConfigureAwait(false);

            id = new LinkId(response.RevisionIdentity.LinkId);
            revisionId = new RevisionId(response.RevisionIdentity.RevisionId);
        }
        catch (ProtonApiException<RevisionConflictResponse> ex) when (ex.Response is { Conflict: { DraftClientId: not null, DraftRevisionId: not null } })
        {
            if (ex.Response.Conflict.DraftClientId != client.Id)
            {
                throw;
            }

            id = new LinkId(ex.Response.Conflict.LinkId);
            revisionId = new RevisionId(ex.Response.Conflict.DraftRevisionId);
        }

        client.SecretsCache.Set(GetNodeKeyCacheKey(parentFolder.VolumeId, id), key.ToBytes());
        client.SecretsCache.Set(GetNameSessionKeyCacheKey(parentFolder.VolumeId, id), nameSessionKey.Export().Token);
        client.SecretsCache.Set(GetPassphraseSessionKeyCacheKey(parentFolder.VolumeId, id), passphraseSessionKey.Export().Token);
        client.SecretsCache.Set(GetContentKeyCacheKey(parentFolder.VolumeId, id), contentKeyToken);

        var file = new FileNode(parentFolder.VolumeId, id, parentFolder.Id, name, nameHashDigest, NodeState.Draft);

        var draftRevision = new Revision(file.VolumeId, id, revisionId, RevisionState.Draft, 0, default);

        return (file, draftRevision);
    }

    internal static async Task<Revision[]> GetRevisionsAsync(ProtonDriveClient client, ShareId shareId, INodeIdentity file, CancellationToken cancellationToken)
    {
        var fileKey = await GetKeyAsync(client, shareId, file.VolumeId, file.Id, cancellationToken).ConfigureAwait(false);

        var response = await client.FilesApi.GetRevisionsAsync(shareId, file.Id, cancellationToken).ConfigureAwait(false);

        return response.Revisions.Select(
            dto =>
            {
                // TODO: verify signature
                var extendedAttributes = dto.ExtendedAttributes is not null
                    ? JsonSerializer.Deserialize(fileKey.Decrypt(dto.ExtendedAttributes.Value), ProtonDriveApiSerializerContext.Default.ExtendedAttributes)
                    : default;

                return new Revision(file.VolumeId, file.Id, dto, extendedAttributes);
            }).ToArray();
    }

    internal static async Task<Revision> GetRevisionAsync(
        ProtonDriveClient client,
        ShareId shareId,
        INodeIdentity file,
        RevisionId revisionId,
        CancellationToken cancellationToken)
    {
        var fileKey = await GetKeyAsync(client, shareId, file.VolumeId, file.Id, cancellationToken).ConfigureAwait(false);

        var response = await client.FilesApi.GetRevisionAsync(shareId, file.Id, revisionId, 1, 1, true, cancellationToken).ConfigureAwait(false);

        // TODO: verify signature
        var extendedAttributes = response.Revision.ExtendedAttributes is not null
            ? JsonSerializer.Deserialize(
                fileKey.Decrypt(response.Revision.ExtendedAttributes.Value),
                ProtonDriveApiSerializerContext.Default.ExtendedAttributes)
            : default;

        return new Revision(file.VolumeId, file.Id, response.Revision, extendedAttributes);
    }

    internal static async Task<PgpSessionKey> GetContentKeyAsync(
        ProtonDriveClient client,
        ShareId shareId,
        VolumeId volumeId,
        LinkId id,
        CancellationToken cancellationToken)
    {
        if (!client.SecretsCache.TryUse(
            GetContentKeyCacheKey(volumeId, id),
            (token, _) => PgpSessionKey.Import(token, SymmetricCipher.Aes256),
            out var nameKey))
        {
            await GetAsync(client, shareId, id, cancellationToken).ConfigureAwait(false);

            if (!client.SecretsCache.TryUse(
                GetContentKeyCacheKey(volumeId, id),
                (token, _) => PgpSessionKey.Import(token, SymmetricCipher.Aes256),
                out nameKey))
            {
                throw new ProtonApiException($"Could not get content key for {id}");
            }
        }

        return nameKey;
    }
}
