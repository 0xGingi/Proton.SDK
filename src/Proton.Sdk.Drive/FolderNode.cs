using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Proton.Cryptography.Pgp;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Drive.Folders;
using Proton.Sdk.Drive.Links;

namespace Proton.Sdk.Drive;

public sealed class FolderNode(
    VolumeId volumeId,
    LinkId id,
    LinkId? parentId,
    string name,
    ReadOnlyMemory<byte> nameHashDigest,
    NodeState state)
    : Node(volumeId, id, parentId, name, nameHashDigest, state)
{
    internal static async IAsyncEnumerable<Node> GetChildrenAsync(
        ProtonDriveClient client,
        ShareId shareId,
        VolumeId volumeId,
        LinkId folderId,
        bool includeHidden,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var folderKey = await GetKeyAsync(client, shareId, volumeId, folderId, cancellationToken).ConfigureAwait(false);

        var parameters = new FolderChildListParameters { PageIndex = 0, PageSize = FoldersApiClient.FolderChildListingPageSize, ShowAll = includeHidden };

        FolderChildListResponse response;

        do
        {
            response = await client.FoldersApi.GetChildrenAsync(shareId, folderId, parameters, cancellationToken).ConfigureAwait(false);

            foreach (var childLink in response.Links)
            {
                yield return Decrypt(client, childLink, folderKey);
            }

            ++parameters.PageIndex;
        } while (response.Links.Count >= FoldersApiClient.FolderChildListingPageSize);
    }

    internal static async Task TrashChildrenAsync(
        FoldersApiClient client,
        ShareId shareId,
        INodeIdentity folder,
        IEnumerable<LinkId> nodeIds,
        CancellationToken cancellationToken)
    {
        var parameters = new MultipleLinkActionParameters { LinkIds = nodeIds.Select(x => x.Value) };

        await client.TrashChildrenAsync(shareId, folder.Id, parameters, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task DeleteChildrenAsync(
        FoldersApiClient client,
        ShareId shareId,
        INodeIdentity folder,
        IEnumerable<LinkId> nodeIds,
        CancellationToken cancellationToken)
    {
        var parameters = new MultipleLinkActionParameters { LinkIds = nodeIds.Select(x => x.Value) };

        await client.DeleteChildrenAsync(shareId, folder.Id, parameters, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<FolderNode> CreateAsync(
        ProtonDriveClient client,
        IShareForCommand share,
        INodeIdentity parentFolder,
        string name,
        CancellationToken cancellationToken)
    {
        using var parentFolderKey = await GetKeyAsync(client, share.Id, parentFolder.VolumeId, parentFolder.Id, cancellationToken).ConfigureAwait(false);
        using var signingKey = await client.Account.GetAddressPrimaryKeyAsync(share.MembershipAddressId, cancellationToken).ConfigureAwait(false);
        var parentHashKey = await GetHashKeyAsync(client, share.Id, parentFolder.VolumeId, parentFolder.Id, cancellationToken).ConfigureAwait(false);

        var hashKey = RandomNumberGenerator.GetBytes(32);

        GetCommonCreationParameters(
            name,
            parentFolderKey,
            parentHashKey.Span,
            signingKey,
            out var key,
            out var nameSessionKey,
            out var passphraseSessionKey,
            out var encryptedName,
            out var nameHashDigest,
            out var encryptedKeyPassphrase,
            out var keyPassphraseSignature,
            out var armoredKey);

        var parameters = new FolderCreationParameters
        {
            Name = encryptedName,
            NameHashDigest = nameHashDigest,
            ParentLinkId = parentFolder.Id.Value,
            Passphrase = encryptedKeyPassphrase,
            PassphraseSignature = keyPassphraseSignature,
            SignatureEmailAddress = share.MembershipEmailAddress,
            Key = armoredKey,
            HashKey = key.EncryptAndSign(hashKey, key),
        };

        var response = await client.FoldersApi.CreateFolderAsync(share.Id, parameters, cancellationToken).ConfigureAwait(false);

        var id = new LinkId(response.FolderId.Value);

        client.SecretsCache.Set(GetNodeKeyCacheKey(parentFolder.VolumeId, id), key.ToBytes());
        client.SecretsCache.Set(GetNameSessionKeyCacheKey(parentFolder.VolumeId, id), nameSessionKey.Export().Token);
        client.SecretsCache.Set(GetPassphraseSessionKeyCacheKey(parentFolder.VolumeId, id), passphraseSessionKey.Export().Token);
        client.SecretsCache.Set(GetHashKeyCacheKey(parentFolder.VolumeId, id), hashKey);

        return new FolderNode(parentFolder.VolumeId, id, parentFolder.Id, name, nameHashDigest, NodeState.Active);
    }
}
