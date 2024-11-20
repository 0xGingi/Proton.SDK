using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using Proton.Cryptography.Pgp;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Drive.Files;
using Proton.Sdk.Drive.Links;
using Proton.Sdk.Drive.Serialization;

namespace Proton.Sdk.Drive;

public partial class Node : INode
{
    private const int PassphraseRandomBytesLength = 32;

    private const string CacheContextName = "drive.volume";
    private const string CacheValueHolderName = "node";
    private const string CacheNodeKeyValueName = "key";
    private const string CacheNameSessionKeyValueName = "name-session-key";
    private const string CachePassphraseSessionKeyValueName = "passphrase-session-key";
    private const string CacheHashKeyValueName = "hash-key";
    private const string CacheContentKeyValueName = "content-key";

    internal Node(
        NodeIdentity nodeIdentity,
        LinkId? parentId,
        string name,
        ByteString nameHashDigest,
        NodeState state)
    {
        NodeIdentity = nodeIdentity;
        ParentId = parentId;
        Name = name;
        State = state;
        NameHashDigest = nameHashDigest;
    }

    protected Node()
    {
        throw new NotImplementedException();
    }

    public NodeIdentity NodeIdentity { get; }
    public LinkId? ParentId { get; }
    public string Name { get; }
    public ByteString NameHashDigest { get; }
    public NodeState State { get; }

    internal static async Task<INode> GetAsync(
        ProtonDriveClient client,
        ShareId shareId,
        LinkId itemId,
        CancellationToken cancellationToken,
        byte[]? operationId = null)
    {
        var response = await client.LinksApi.GetLinkAsync(shareId, itemId, cancellationToken, operationId).ConfigureAwait(false);

        return await GetAsync(client, shareId, response.Link, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<INode> GetAsync(ProtonDriveClient client, ShareId shareId, Link link, CancellationToken cancellationToken)
    {
        using var parentKey = await GetParentKeyAsync(client, shareId, link, cancellationToken).ConfigureAwait(false);

        return Decrypt(client, link, parentKey);
    }

    internal static async Task MoveAsync(
        ProtonDriveClient client,
        ShareId shareId,
        INode node,
        IShareForCommand destinationShare,
        INodeIdentity destinationFolderIdentity,
        string nameAtDestination,
        CancellationToken cancellationToken)
    {
        using var signingKey = await client.Account.GetAddressPrimaryKeyAsync(destinationShare.MembershipAddressId, cancellationToken).ConfigureAwait(false);
        var nodeIdentity = new NodeIdentity { ShareId = shareId, VolumeId = node.NodeIdentity.VolumeId, NodeId = node.NodeIdentity.NodeId };
        var destinationNodeIdentity = new NodeIdentity { ShareId = shareId, VolumeId = destinationFolderIdentity.VolumeId, NodeId = destinationFolderIdentity.NodeId };

        using var nameSessionKey = await GetNameSessionKeyAsync(client, nodeIdentity, cancellationToken).ConfigureAwait(false);

        using var passphraseSessionKey =
            await GetPassphraseSessionKeyAsync(client, nodeIdentity, cancellationToken).ConfigureAwait(false);

        using var destinationFolderKey = await GetKeyAsync(
            client,
            destinationNodeIdentity,
            cancellationToken).ConfigureAwait(false);

        var destinationFolderHashKey = await GetHashKeyAsync(client, destinationFolderIdentity, cancellationToken)
            .ConfigureAwait(false);

        GetNameParameters(
            nameAtDestination,
            destinationFolderKey,
            destinationFolderHashKey.Span,
            nameSessionKey,
            signingKey,
            out var encryptedName,
            out var nameHashDigest);

        var keyPassphraseKeyPacket = destinationFolderKey.EncryptSessionKey(passphraseSessionKey);

        var parameters = new MoveLinkParameters
        {
            ShareId = destinationShare.ShareId.Value,
            ParentLinkId = destinationFolderIdentity.NodeId.Value,
            KeyPassphrase = keyPassphraseKeyPacket,
            Name = encryptedName,
            NameHashDigest = nameHashDigest,
            NameSignatureEmailAddress = destinationShare.MembershipEmailAddress,
            OriginalNameHashDigest = node.NameHashDigest.Memory,
        };

        await client.LinksApi.MoveLinkAsync(shareId, node.NodeIdentity.NodeId, parameters, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task RenameAsync(
        ProtonDriveClient client,
        IShareForCommand share,
        INode node,
        string newName,
        string newMediaType,
        CancellationToken cancellationToken)
    {
        var nodeIdentity = new NodeIdentity { ShareId = share.ShareId, VolumeId = node.NodeIdentity.VolumeId, NodeId = node.NodeIdentity.NodeId };

        // FIXME: Can ParentId be null? What then
        var parentNodeIdentity = new NodeIdentity { ShareId = share.ShareId, VolumeId = node.NodeIdentity.VolumeId, NodeId = node.ParentId! };

        using var signingKey = await client.Account.GetAddressPrimaryKeyAsync(new AddressId(share.MembershipAddressId), cancellationToken).ConfigureAwait(false);
        using var nameSessionKey = await GetNameSessionKeyAsync(client, nodeIdentity, cancellationToken).ConfigureAwait(false);
        using var parentFolderKey = await GetKeyAsync(client, parentNodeIdentity, cancellationToken).ConfigureAwait(false);
        var parentFolderHashKey = await GetHashKeyAsync(client, parentNodeIdentity, cancellationToken).ConfigureAwait(false);

        GetNameParameters(newName, parentFolderKey, parentFolderHashKey.Span, nameSessionKey, signingKey, out var encryptedName, out var nameHashDigest);

        var parameters = new RenameLinkParameters
        {
            Name = encryptedName,
            NameHashDigest = nameHashDigest,
            NameSignatureEmailAddress = share.MembershipEmailAddress,
            MediaType = newMediaType,
            OriginalNameHashDigest = node.NameHashDigest.Memory,
        };

        await client.LinksApi.RenameLinkAsync(share.ShareId, nodeIdentity.NodeId, parameters, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<PgpPrivateKey> GetKeyAsync(
        ProtonDriveClient client,
        INodeIdentity nodeIdentity,
        CancellationToken cancellationToken,
        byte[]? operationId = null)
    {
        if (!client.SecretsCache.TryUse(GetNodeKeyCacheKey(nodeIdentity.VolumeId, nodeIdentity.NodeId), (data, _) => PgpPrivateKey.Import(data), out var key))
        {
            await GetAsync(client, nodeIdentity.ShareId, nodeIdentity.NodeId, cancellationToken, operationId).ConfigureAwait(false);

            if (!client.SecretsCache.TryUse(GetNodeKeyCacheKey(nodeIdentity.VolumeId, nodeIdentity.NodeId), (data, _) => PgpPrivateKey.Import(data), out key))
            {
                throw new ProtonApiException($"Could not get node key for {nodeIdentity.NodeId}");
            }
        }

        return key;
    }

    internal static CacheKey GetNodeKeyCacheKey(VolumeId volumeId, LinkId nodeId)
        => new(CacheContextName, volumeId.Value, CacheValueHolderName, nodeId.Value, CacheNodeKeyValueName);

    internal static CacheKey GetNameSessionKeyCacheKey(VolumeId volumeId, LinkId nodeId)
        => new(CacheContextName, volumeId.Value, CacheValueHolderName, nodeId.Value, CacheNameSessionKeyValueName);

    internal static CacheKey GetPassphraseSessionKeyCacheKey(VolumeId volumeId, LinkId nodeId)
        => new(CacheContextName, volumeId.Value, CacheValueHolderName, nodeId.Value, CachePassphraseSessionKeyValueName);

    internal static CacheKey GetHashKeyCacheKey(VolumeId volumeId, LinkId nodeId)
        => new(CacheContextName, volumeId.Value, CacheValueHolderName, nodeId.Value, CacheHashKeyValueName);

    internal static CacheKey GetContentKeyCacheKey(VolumeId volumeId, LinkId nodeId)
        => new(CacheContextName, volumeId.Value, CacheValueHolderName, nodeId.Value, CacheContentKeyValueName);

    internal static void GetCommonCreationParameters(
        string name,
        PgpPrivateKey parentFolderKey,
        ReadOnlySpan<byte> parentFolderHashKey,
        PgpPrivateKey signingKey,
        out PgpPrivateKey key,
        out PgpSessionKey nameSessionKey,
        out PgpSessionKey passphraseSessionKey,
        out ArraySegment<byte> encryptedName,
        out ArraySegment<byte> nameHashDigest,
        out ArraySegment<byte> encryptedKeyPassphrase,
        out ArraySegment<byte> passphraseSignature,
        out ArraySegment<byte> lockedKeyBytes)
    {
        key = PgpPrivateKey.Generate("Drive key", "no-reply@proton.me", KeyGenerationAlgorithm.Default);
        nameSessionKey = PgpSessionKey.Generate();

        Span<byte> passphrase = stackalloc byte[Base64.GetMaxEncodedToUtf8Length(PassphraseRandomBytesLength)];
        var passphraseRandomBytes = passphrase[..PassphraseRandomBytesLength];
        RandomNumberGenerator.Fill(passphraseRandomBytes);
        Base64.EncodeToUtf8InPlace(passphrase, PassphraseRandomBytesLength, out var passphraseLength);
        passphrase = passphrase[..passphraseLength];

        passphraseSessionKey = PgpSessionKey.Generate();
        var passphraseEncryptionSecrets = new EncryptionSecrets(parentFolderKey, passphraseSessionKey);

        encryptedKeyPassphrase = PgpEncrypter.EncryptAndSign(passphrase, passphraseEncryptionSecrets, signingKey, out passphraseSignature);

        using var lockedKey = key.Lock(passphrase);
        lockedKeyBytes = lockedKey.ToBytes();

        GetNameParameters(name, parentFolderKey, parentFolderHashKey, nameSessionKey, signingKey, out encryptedName, out nameHashDigest);
    }

    internal static INode Decrypt(ProtonDriveClient client, Link link, PgpPrivateKey parentKey)
    {
        var secretsCache = client.SecretsCache;

        var volumeId = new VolumeId(link.VolumeId);
        var linkId = new LinkId(link.Id);
        var parentId = link.ParentId is not null ? new LinkId(link.ParentId) : default(LinkId?);
        var state = (NodeState)link.State;

        using var nameSessionKey = parentKey.DecryptSessionKey(link.Name);
        secretsCache.Set(GetNameSessionKeyCacheKey(volumeId, linkId), nameSessionKey.Export().Token);

        var name = nameSessionKey.DecryptText(link.Name);

        using var passphraseSessionKey = parentKey.DecryptSessionKey(link.Passphrase);
        secretsCache.Set(GetPassphraseSessionKeyCacheKey(volumeId, linkId), passphraseSessionKey.Export().Token);

        var passphrase = passphraseSessionKey.Decrypt(link.Passphrase);

        using var key = PgpPrivateKey.ImportAndUnlock(link.Key, passphrase);
        secretsCache.Set(GetNodeKeyCacheKey(volumeId, linkId), key.ToBytes());

        if (link.Type == LinkType.Folder)
        {
            var folderProperties = link.FolderProperties ?? throw new ProtonApiException("Missing folder properties on link of type Folder.");

            var hashKeyMessage = folderProperties.HashKey;

            var hashKey = key.Decrypt(hashKeyMessage);
            secretsCache.Set(GetHashKeyCacheKey(volumeId, linkId), hashKey);

            return new FolderNode
            {
                NodeIdentity = new NodeIdentity
                {
                    VolumeId = volumeId,
                    NodeId = linkId,
                },
                ParentId = parentId,
                Name = name,
                NameHashDigest = ByteString.CopyFrom(link.NameHashDigest.ToArray()),
                State = state
            };
        }

        var fileProperties = link.FileProperties ?? throw new ProtonApiException("Missing file properties on link of type File.");

        using var contentKey = key.DecryptSessionKey(fileProperties.ContentKeyPacket.Span);
        secretsCache.Set(GetContentKeyCacheKey(volumeId, linkId), contentKey.Export().Token);

        var extendedAttributes = link.ExtendedAttributes is not null
            ?
            JsonSerializer.Deserialize(key.Decrypt(link.ExtendedAttributes.Value), ProtonDriveApiSerializerContext.Default.ExtendedAttributes)
            :
            default;

        var activeRevisionDto = fileProperties.ActiveRevision is not null
            ? (fileProperties.ActiveRevision, extendedAttributes)
            : default((RevisionDto, ExtendedAttributes)?);

        return new FileNode(
            new NodeIdentity { NodeId = linkId, VolumeId = volumeId },
            parentId,
            name,
            ByteString.CopyFrom(link.NameHashDigest.ToArray()),
            state,
            activeRevisionDto);
    }

    internal static async Task<ReadOnlyMemory<byte>> GetHashKeyAsync(
        ProtonDriveClient client,
        INodeIdentity nodeIdentity,
        CancellationToken cancellationToken)
    {
        if (!client.SecretsCache.TryUse(GetHashKeyCacheKey(nodeIdentity.VolumeId, nodeIdentity.NodeId), (data, _) => data.ToArray().AsMemory(), out var hashKey))
        {
            await GetAsync(client, nodeIdentity.ShareId, nodeIdentity.NodeId, cancellationToken).ConfigureAwait(false);

            if (!client.SecretsCache.TryUse(GetHashKeyCacheKey(nodeIdentity.VolumeId, nodeIdentity.NodeId), (data, _) => data.ToArray().AsMemory(), out hashKey))
            {
                throw new ProtonApiException($"Could not get hash key for {nodeIdentity.NodeId}");
            }
        }

        return hashKey;
    }

    private static void GetNameParameters(
        string name,
        PgpPrivateKey parentFolderKey,
        ReadOnlySpan<byte> parentFolderHashKey,
        PgpSessionKey nameSessionKey,
        PgpPrivateKey signingKey,
        out ArraySegment<byte> encryptedName,
        out ArraySegment<byte> nameHashDigest)
    {
        var maxNameByteLength = Encoding.UTF8.GetByteCount(name);
        var nameBytes = MemoryProvider.GetHeapMemoryIfTooLargeForStack(maxNameByteLength, out var nameHeapMemoryOwner)
            ? nameHeapMemoryOwner.Memory.Span
            : stackalloc byte[maxNameByteLength];

        using (nameHeapMemoryOwner)
        {
            var nameByteLength = Encoding.UTF8.GetBytes(name, nameBytes);
            nameBytes = nameBytes[..nameByteLength];

            encryptedName = PgpEncrypter.EncryptAndSignText(name, new EncryptionSecrets(parentFolderKey, nameSessionKey), signingKey);

            nameHashDigest = HMACSHA256.HashData(parentFolderHashKey, nameBytes);
        }
    }

    private static async Task<PgpPrivateKey> GetParentKeyAsync(ProtonDriveClient client, ShareId shareId, Link link, CancellationToken cancellationToken)
    {
        if (link.ParentId is null)
        {
            return await Share.GetKeyAsync(client, shareId, cancellationToken).ConfigureAwait(false);
        }

        var volumeId = new VolumeId(link.VolumeId);

        if (!client.SecretsCache.TryUse(GetNodeKeyCacheKey(volumeId, new LinkId(link.ParentId)), (bytes, _) => PgpPrivateKey.Import(bytes), out var key))
        {
            var linkAncestry = new Stack<Link>(8);

            var currentId = (LinkId?)new LinkId(link.ParentId);

            while (currentId is not null)
            {
                var response = await client.LinksApi.GetLinkAsync(shareId, currentId, cancellationToken).ConfigureAwait(false);

                if (client.SecretsCache.TryUse(
                    GetNodeKeyCacheKey(volumeId, new LinkId(response.Link.Id)),
                    (bytes, _) => PgpPrivateKey.Import(bytes),
                    out key))
                {
                    break;
                }

                var ancestorLink = response.Link;

                linkAncestry.Push(ancestorLink);

                currentId = ancestorLink.ParentId is not null ? new LinkId(ancestorLink.ParentId) : null;

                if (currentId is null)
                {
                    key = await Share.GetKeyAsync(client, shareId, cancellationToken).ConfigureAwait(false);
                }
            }

            while (linkAncestry.TryPop(out var ancestorLink))
            {
                var ancestorKeyPassphrase = key.Decrypt(ancestorLink.Passphrase);

                key = PgpPrivateKey.ImportAndUnlock(ancestorLink.Key, ancestorKeyPassphrase);

                client.SecretsCache.Set(GetNodeKeyCacheKey(volumeId, new LinkId(ancestorLink.Id)), key.ToBytes());
            }
        }

        return key;
    }

    private static async Task<PgpSessionKey> GetNameSessionKeyAsync(
        ProtonDriveClient client,
        NodeIdentity nodeIdentity,
        CancellationToken cancellationToken)
    {
        if (!client.SecretsCache.TryUse(
            GetNameSessionKeyCacheKey(nodeIdentity.VolumeId, nodeIdentity.NodeId),
            (token, _) => PgpSessionKey.Import(token, SymmetricCipher.Aes256),
            out var nameKey))
        {
            await GetAsync(client, nodeIdentity.ShareId, nodeIdentity.NodeId, cancellationToken).ConfigureAwait(false);

            if (!client.SecretsCache.TryUse(
                GetNameSessionKeyCacheKey(nodeIdentity.VolumeId, nodeIdentity.NodeId),
                (token, _) => PgpSessionKey.Import(token, SymmetricCipher.Aes256),
                out nameKey))
            {
                throw new ProtonApiException($"Could not get name session key for {nodeIdentity.NodeId}");
            }
        }

        return nameKey;
    }

    private static async Task<PgpSessionKey> GetPassphraseSessionKeyAsync(
        ProtonDriveClient client,
        NodeIdentity nodeIdentity,
        CancellationToken cancellationToken)
    {
        if (!client.SecretsCache.TryUse(
            GetPassphraseSessionKeyCacheKey(nodeIdentity.VolumeId, nodeIdentity.NodeId),
            (token, _) => PgpSessionKey.Import(token, SymmetricCipher.Aes256),
            out var passphraseKey))
        {
            await GetAsync(client, nodeIdentity.ShareId, nodeIdentity.NodeId, cancellationToken).ConfigureAwait(false);

            if (!client.SecretsCache.TryUse(
                GetPassphraseSessionKeyCacheKey(nodeIdentity.VolumeId, nodeIdentity.NodeId),
                (token, _) => PgpSessionKey.Import(token, SymmetricCipher.Aes256),
                out passphraseKey))
            {
                throw new ProtonApiException($"Could not get passphrase session key for {nodeIdentity.NodeId}");
            }
        }

        return passphraseKey;
    }
}
