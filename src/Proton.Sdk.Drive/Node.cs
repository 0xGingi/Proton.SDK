using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Proton.Cryptography.Pgp;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Drive.Files;
using Proton.Sdk.Drive.Links;
using Proton.Sdk.Drive.Serialization;

namespace Proton.Sdk.Drive;

public abstract class Node : INodeForMove
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
        VolumeId volumeId,
        LinkId id,
        LinkId? parentId,
        string name,
        ReadOnlyMemory<byte> nameHashDigest,
        NodeState state)
    {
        VolumeId = volumeId;
        Id = id;
        ParentId = parentId;
        Name = name;
        State = state;
        NameHashDigest = nameHashDigest;
    }

    public VolumeId VolumeId { get; }

    public LinkId Id { get; }

    public LinkId? ParentId { get; }

    public string Name { get; }

    public NodeState State { get; }

    public ReadOnlyMemory<byte> NameHashDigest { get; }

    internal static async Task<Node> GetAsync(ProtonDriveClient client, ShareId shareId, LinkId itemId, CancellationToken cancellationToken)
    {
        var response = await client.LinksApi.GetLinkAsync(shareId, itemId, cancellationToken).ConfigureAwait(false);

        return await GetAsync(client, shareId, response.Link, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<Node> GetAsync(ProtonDriveClient client, ShareId shareId, Link link, CancellationToken cancellationToken)
    {
        using var parentKey = await GetParentKeyAsync(client, shareId, link, cancellationToken).ConfigureAwait(false);

        return Decrypt(client, link, parentKey);
    }

    internal static async Task MoveAsync(
        ProtonDriveClient client,
        ShareId shareId,
        INodeForMove node,
        LinkId parentFolderId,
        IShareForCommand destinationShare,
        INodeIdentity destinationParentFolder,
        string nameAtDestination,
        CancellationToken cancellationToken)
    {
        using var signingKey = await client.Account.GetAddressPrimaryKeyAsync(destinationShare.MembershipAddressId, cancellationToken).ConfigureAwait(false);
        using var nameSessionKey = await GetNameSessionKeyAsync(client, shareId, node.VolumeId, node.Id, cancellationToken).ConfigureAwait(false);

        using var passphraseSessionKey =
            await GetPassphraseSessionKeyAsync(client, shareId, node.VolumeId, node.Id, cancellationToken).ConfigureAwait(false);

        using var destinationParentFolderKey = await GetKeyAsync(
            client,
            shareId,
            destinationParentFolder.VolumeId,
            destinationParentFolder.Id,
            cancellationToken).ConfigureAwait(false);

        var destinationParentHashKey = await GetHashKeyAsync(client, shareId, node.VolumeId, destinationParentFolder.Id, cancellationToken)
            .ConfigureAwait(false);

        GetNameParameters(
            nameAtDestination,
            destinationParentFolderKey,
            destinationParentHashKey.Span,
            nameSessionKey,
            signingKey,
            out var encryptedName,
            out var nameHashDigest);

        var keyPassphraseKeyPacket = destinationParentFolderKey.EncryptSessionKey(passphraseSessionKey);

        var parameters = new MoveLinkParameters
        {
            ShareId = destinationShare.Id.Value,
            ParentLinkId = destinationParentFolder.Id.Value,
            KeyPassphrase = keyPassphraseKeyPacket,
            Name = encryptedName,
            NameHashDigest = nameHashDigest,
            NameSignatureEmailAddress = destinationShare.MembershipEmailAddress,
            OriginalNameHashDigest = node.NameHashDigest,
        };

        await client.LinksApi.MoveLinkAsync(shareId, node.Id, parameters, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task RenameAsync(
        ProtonDriveClient client,
        IShareForCommand share,
        INodeForRename node,
        LinkId parentFolderId,
        string newName,
        string newMediaType,
        CancellationToken cancellationToken)
    {
        using var signingKey = await client.Account.GetAddressPrimaryKeyAsync(share.MembershipAddressId, cancellationToken).ConfigureAwait(false);
        using var nameSessionKey = await GetNameSessionKeyAsync(client, share.Id, node.VolumeId, node.Id, cancellationToken).ConfigureAwait(false);
        using var parentFolderKey = await GetKeyAsync(client, share.Id, node.VolumeId, parentFolderId, cancellationToken).ConfigureAwait(false);
        var parentFolderHashKey = await GetHashKeyAsync(client, share.Id, node.VolumeId, parentFolderId, cancellationToken).ConfigureAwait(false);

        GetNameParameters(newName, parentFolderKey, parentFolderHashKey.Span, nameSessionKey, signingKey, out var encryptedName, out var nameHashDigest);

        var parameters = new RenameLinkParameters
        {
            Name = encryptedName,
            NameHashDigest = nameHashDigest,
            NameSignatureEmailAddress = share.MembershipEmailAddress,
            MediaType = newMediaType,
            OriginalNameHashDigest = node.NameHashDigest,
        };

        await client.LinksApi.RenameLinkAsync(share.Id, node.Id, parameters, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<PgpPrivateKey> GetKeyAsync(
        ProtonDriveClient client,
        ShareId shareId,
        VolumeId volumeId,
        LinkId id,
        CancellationToken cancellationToken)
    {
        if (!client.SecretsCache.TryUse(GetNodeKeyCacheKey(volumeId, id), (data, _) => PgpPrivateKey.Import(data), out var key))
        {
            await GetAsync(client, shareId, id, cancellationToken).ConfigureAwait(false);

            if (!client.SecretsCache.TryUse(GetNodeKeyCacheKey(volumeId, id), (data, _) => PgpPrivateKey.Import(data), out key))
            {
                throw new ProtonApiException($"Could not get node key for {id}");
            }
        }

        return key;
    }

    private protected static CacheKey GetNodeKeyCacheKey(VolumeId volumeId, LinkId nodeId)
        => new(CacheContextName, volumeId.Value, CacheValueHolderName, nodeId.Value, CacheNodeKeyValueName);

    private protected static CacheKey GetNameSessionKeyCacheKey(VolumeId volumeId, LinkId nodeId)
        => new(CacheContextName, volumeId.Value, CacheValueHolderName, nodeId.Value, CacheNameSessionKeyValueName);

    private protected static CacheKey GetPassphraseSessionKeyCacheKey(VolumeId volumeId, LinkId nodeId)
        => new(CacheContextName, volumeId.Value, CacheValueHolderName, nodeId.Value, CachePassphraseSessionKeyValueName);

    private protected static CacheKey GetHashKeyCacheKey(VolumeId volumeId, LinkId nodeId)
        => new(CacheContextName, volumeId.Value, CacheValueHolderName, nodeId.Value, CacheHashKeyValueName);

    private protected static CacheKey GetContentKeyCacheKey(VolumeId volumeId, LinkId nodeId)
        => new(CacheContextName, volumeId.Value, CacheValueHolderName, nodeId.Value, CacheContentKeyValueName);

    private protected static void GetCommonCreationParameters(
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

    private protected static Node Decrypt(ProtonDriveClient client, Link link, PgpPrivateKey parentKey)
    {
        var secretsCache = client.SecretsCache;

        var volumeId = new VolumeId(link.VolumeId);
        var id = new LinkId(link.Id);
        var parentId = link.ParentId is not null ? new LinkId(link.ParentId) : default(LinkId?);
        var state = (NodeState)link.State;

        using var nameSessionKey = parentKey.DecryptSessionKey(link.Name);
        secretsCache.Set(GetNameSessionKeyCacheKey(volumeId, id), nameSessionKey.Export().Token);

        var name = nameSessionKey.DecryptText(link.Name);

        using var passphraseSessionKey = parentKey.DecryptSessionKey(link.Passphrase);
        secretsCache.Set(GetPassphraseSessionKeyCacheKey(volumeId, id), passphraseSessionKey.Export().Token);

        var passphrase = passphraseSessionKey.Decrypt(link.Passphrase);

        using var key = PgpPrivateKey.ImportAndUnlock(link.Key, passphrase);
        secretsCache.Set(GetNodeKeyCacheKey(volumeId, id), key.ToBytes());

        if (link.Type == LinkType.Folder)
        {
            var folderProperties = link.FolderProperties ?? throw new ProtonApiException("Missing folder properties on link of type Folder.");

            var hashKeyMessage = folderProperties.HashKey;

            var hashKey = key.Decrypt(hashKeyMessage);
            secretsCache.Set(GetHashKeyCacheKey(volumeId, id), hashKey);

            return new FolderNode(volumeId, id, parentId, name, link.NameHashDigest, state);
        }

        var fileProperties = link.FileProperties ?? throw new ProtonApiException("Missing file properties on link of type File.");

        using var contentKey = key.DecryptSessionKey(fileProperties.ContentKeyPacket.Span);
        secretsCache.Set(GetContentKeyCacheKey(volumeId, id), contentKey.Export().Token);

        var extendedAttributes = link.ExtendedAttributes is not null
            ? JsonSerializer.Deserialize(key.Decrypt(link.ExtendedAttributes.Value), ProtonDriveApiSerializerContext.Default.ExtendedAttributes)
            : default;

        var activeRevision = fileProperties.ActiveRevision is not null
            ? (fileProperties.ActiveRevision, extendedAttributes)
            : default((RevisionDto, ExtendedAttributes)?);

        return new FileNode(volumeId, id, parentId, name, link.NameHashDigest, state, activeRevision);
    }

    private protected static async Task<ReadOnlyMemory<byte>> GetHashKeyAsync(
        ProtonDriveClient client,
        ShareId shareId,
        VolumeId volumeId,
        LinkId id,
        CancellationToken cancellationToken)
    {
        if (!client.SecretsCache.TryUse(GetHashKeyCacheKey(volumeId, id), (data, _) => data.ToArray().AsMemory(), out var hashKey))
        {
            await GetAsync(client, shareId, id, cancellationToken).ConfigureAwait(false);

            if (!client.SecretsCache.TryUse(GetHashKeyCacheKey(volumeId, id), (data, _) => data.ToArray().AsMemory(), out hashKey))
            {
                throw new ProtonApiException($"Could not get hash key for {id}");
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
                var response = await client.LinksApi.GetLinkAsync(shareId, currentId.Value, cancellationToken).ConfigureAwait(false);

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
        ShareId shareId,
        VolumeId volumeId,
        LinkId id,
        CancellationToken cancellationToken)
    {
        if (!client.SecretsCache.TryUse(
            GetNameSessionKeyCacheKey(volumeId, id),
            (token, _) => PgpSessionKey.Import(token, SymmetricCipher.Aes256),
            out var nameKey))
        {
            await GetAsync(client, shareId, id, cancellationToken).ConfigureAwait(false);

            if (!client.SecretsCache.TryUse(
                GetNameSessionKeyCacheKey(volumeId, id),
                (token, _) => PgpSessionKey.Import(token, SymmetricCipher.Aes256),
                out nameKey))
            {
                throw new ProtonApiException($"Could not get name session key for {id}");
            }
        }

        return nameKey;
    }

    private static async Task<PgpSessionKey> GetPassphraseSessionKeyAsync(
        ProtonDriveClient client,
        ShareId shareId,
        VolumeId volumeId,
        LinkId id,
        CancellationToken cancellationToken)
    {
        if (!client.SecretsCache.TryUse(
            GetPassphraseSessionKeyCacheKey(volumeId, id),
            (token, _) => PgpSessionKey.Import(token, SymmetricCipher.Aes256),
            out var passphraseKey))
        {
            await GetAsync(client, shareId, id, cancellationToken).ConfigureAwait(false);

            if (!client.SecretsCache.TryUse(
                GetPassphraseSessionKeyCacheKey(volumeId, id),
                (token, _) => PgpSessionKey.Import(token, SymmetricCipher.Aes256),
                out passphraseKey))
            {
                throw new ProtonApiException($"Could not get passphrase session key for {id}");
            }
        }

        return passphraseKey;
    }
}
