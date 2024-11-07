using System.Text.Json;
using Google.Protobuf;
using Proton.Cryptography.Pgp;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Drive.Files;
using Proton.Sdk.Drive.Serialization;

namespace Proton.Sdk.Drive;

public sealed partial class FileNode : INode
    {
    internal FileNode(
        NodeIdentity nodeIdentity,
        LinkId? parentId,
        string name,
        ByteString nameHashDigest,
        NodeState state,
        (RevisionDto Properties, ExtendedAttributes ExtendedAttributes)? activeRevision = null)
    {
        NodeIdentity = nodeIdentity;
        ParentId = parentId;
        Name = name;
        NameHashDigest = nameHashDigest;
        State = state;

        if (activeRevision is not null)
        {
            var (activeRevisionProperties, extendedAttributes) = activeRevision.Value;
            ActiveRevision = new Revision(nodeIdentity.VolumeId, nodeIdentity.NodeId, activeRevisionProperties, extendedAttributes);
        }
    }

    internal static async Task<FileCreationResponse> CreateFileAsync(
        ProtonDriveClient client,
        FileCreationRequest fileCreationRequest,
        CancellationToken cancellationToken)
    {
        var parentFolderIdentity = new NodeIdentity
        {
            NodeId = fileCreationRequest.ParentFolderIdentity.NodeId,
            VolumeId = fileCreationRequest.ParentFolderIdentity.VolumeId,
            ShareId = fileCreationRequest.ShareMetadata.ShareId,
        };
        var parentFolderKey = await Node.GetKeyAsync(client, parentFolderIdentity, cancellationToken).ConfigureAwait(false);

        var signingKey = await client.Account.GetAddressPrimaryKeyAsync(
            fileCreationRequest.ShareMetadata.MembershipAddressId,
            cancellationToken
        ).ConfigureAwait(false);

        var parentFolderHashKey = await Node.GetHashKeyAsync(
                client,
                parentFolderIdentity,
                cancellationToken)
            .ConfigureAwait(false);

        Node.GetCommonCreationParameters(
            fileCreationRequest.Name,
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
            ClientId = client.ClientId,
            Name = encryptedName,
            NameHashDigest = nameHashDigest,
            ParentLinkId = fileCreationRequest.ParentFolderIdentity.NodeId.Value,
            Passphrase = encryptedKeyPassphrase,
            PassphraseSignature = passphraseSignature,
            SignatureEmailAddress = fileCreationRequest.ShareMetadata.MembershipEmailAddress,
            Key = lockedKeyBytes,
            MediaType = fileCreationRequest.MimeType,
            ContentKeyPacket = key.EncryptSessionKey(contentKey),
            ContentKeyPacketSignature = key.Sign(contentKeyToken),
        };

        LinkId createdNodeId;
        RevisionId createdRevisionId;
        try
        {
            var response = await client.FilesApi.CreateFileAsync(parentFolderIdentity.ShareId, parameters, cancellationToken).ConfigureAwait(false);

            createdNodeId = new LinkId(response.Identities.LinkId);
            createdRevisionId = new RevisionId(response.Identities.RevisionId);

            client.SecretsCache.Set(Node.GetNodeKeyCacheKey(parentFolderIdentity.VolumeId, createdNodeId), key.ToBytes());
            client.SecretsCache.Set(Node.GetNameSessionKeyCacheKey(parentFolderIdentity.VolumeId, createdNodeId), nameSessionKey.Export().Token);
            client.SecretsCache.Set(Node.GetPassphraseSessionKeyCacheKey(parentFolderIdentity.VolumeId, createdNodeId), passphraseSessionKey.Export().Token);
            client.SecretsCache.Set(Node.GetContentKeyCacheKey(parentFolderIdentity.VolumeId, createdNodeId), contentKeyToken);
        }
        catch (ProtonApiException<RevisionConflictResponse> ex) when (ex.Response is { Conflict: { DraftClientId: not null, DraftRevisionId: not null } })
        {
            if (ex.Response.Conflict.DraftClientId != client.ClientId)
            {
                throw;
            }

            createdNodeId = new LinkId(ex.Response.Conflict.LinkId);
            createdRevisionId = new RevisionId(ex.Response.Conflict.DraftRevisionId);
        }

        var file = new FileNode
        {
            NodeIdentity = new NodeIdentity
            {
                VolumeId = parentFolderIdentity.VolumeId,
                NodeId = createdNodeId,
                ShareId = parentFolderIdentity.ShareId,
            },
            ParentId = fileCreationRequest.ParentFolderIdentity.NodeId,
            Name = fileCreationRequest.Name,
            NameHashDigest = ByteString.CopyFrom(nameHashDigest),
            State = NodeState.Draft,
        };

        var draftRevision = new Revision(
            file.NodeIdentity.VolumeId,
            createdNodeId,
            createdRevisionId,
            RevisionState.Draft,
            0,
            default);

        return new FileCreationResponse
        {
            File = file,
            Revision = draftRevision,
        };
    }

    internal static async Task<Revision[]> GetFileRevisionsAsync(ProtonDriveClient client, INodeIdentity fileNodeIdentity, CancellationToken cancellationToken)
    {
        var fileKey = await Node.GetKeyAsync(client, fileNodeIdentity, cancellationToken).ConfigureAwait(false);

        var response = await client.FilesApi.GetRevisionsAsync(fileNodeIdentity.ShareId, fileNodeIdentity.NodeId, cancellationToken).ConfigureAwait(false);

        return response.Revisions.Select(
            dto =>
            {
                // TODO: verify signature
                var extendedAttributes = dto.ExtendedAttributes is not null
                    ? JsonSerializer.Deserialize(fileKey.Decrypt(dto.ExtendedAttributes.Value), ProtonDriveApiSerializerContext.Default.ExtendedAttributes)
                    : default;

                return new Revision(fileNodeIdentity.VolumeId, fileNodeIdentity.NodeId, dto, extendedAttributes);
            }).ToArray();
    }

    internal static async Task<Revision> GetFileRevisionAsync(
        ProtonDriveClient client,
        NodeIdentity fileNodeIdentity,
        RevisionId revisionId,
        CancellationToken cancellationToken)
    {
        var fileKey = await Node.GetKeyAsync(client, fileNodeIdentity, cancellationToken).ConfigureAwait(false);

        var response = await client.FilesApi.GetRevisionAsync(fileNodeIdentity.ShareId, fileNodeIdentity.NodeId, revisionId, 1, 1, true, cancellationToken)
            .ConfigureAwait(false);

        // TODO: verify signature
        var extendedAttributes = response.Revision.ExtendedAttributes is not null
            ? JsonSerializer.Deserialize(
                fileKey.Decrypt(response.Revision.ExtendedAttributes.Value),
                ProtonDriveApiSerializerContext.Default.ExtendedAttributes)
            : default;

        return new Revision(
            fileNodeIdentity.VolumeId,
            fileNodeIdentity.NodeId,
            response.Revision,
            extendedAttributes);
    }

    internal static async Task<PgpSessionKey> GetFileContentKeyAsync(
        ProtonDriveClient client,
        INodeIdentity nodeIdentity,
        CancellationToken cancellationToken)
    {
        if (!client.SecretsCache.TryUse(
            Node.GetContentKeyCacheKey(nodeIdentity.VolumeId, nodeIdentity.NodeId),
            (token, _) => PgpSessionKey.Import(token, SymmetricCipher.Aes256),
            out var nameKey))
        {
            await Node.GetAsync(client, nodeIdentity.ShareId, nodeIdentity.NodeId, cancellationToken).ConfigureAwait(false);

            if (!client.SecretsCache.TryUse(
                Node.GetContentKeyCacheKey(nodeIdentity.VolumeId, nodeIdentity.NodeId),
                (token, _) => PgpSessionKey.Import(token, SymmetricCipher.Aes256),
                out nameKey))
            {
                throw new ProtonApiException($"Could not get content key for {nodeIdentity.NodeId}");
            }
        }

        return nameKey;
    }
}
