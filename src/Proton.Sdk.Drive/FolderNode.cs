using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Google.Protobuf;
using Proton.Cryptography.Pgp;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Drive.Folders;
using Proton.Sdk.Drive.Links;

namespace Proton.Sdk.Drive;

public sealed partial class FolderNode : INode
{
    internal FolderNode(
        NodeIdentity nodeIdentity,
        LinkId? parentId,
        string name,
        ByteString nameHashDigest,
        NodeState state)
    {
        NodeIdentity = nodeIdentity;
        ParentId = parentId;
        Name = name;
        NameHashDigest = nameHashDigest;
        State = state;
    }

    internal static async IAsyncEnumerable<INode> GetFolderChildrenAsync(
        ProtonDriveClient client,
        INodeIdentity folderIdentity,
        bool includeHidden,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var folderKey = await Node.GetKeyAsync(client, folderIdentity, cancellationToken).ConfigureAwait(false);

        var parameters = new FolderChildListParameters { PageIndex = 0, PageSize = FoldersApiClient.FolderChildListingPageSize, ShowAll = includeHidden };

        FolderChildListResponse response;

        do
        {
            response = await client.FoldersApi.GetChildrenAsync(folderIdentity.ShareId, folderIdentity.NodeId, parameters, cancellationToken).ConfigureAwait(false);

            foreach (var childLink in response.Links)
            {
                yield return Node.Decrypt(client, childLink, folderKey);
            }

            ++parameters.PageIndex;
        } while (response.Links.Count >= FoldersApiClient.FolderChildListingPageSize);
    }

    internal static async Task TrashFolderChildrenAsync(
        FoldersApiClient client,
        INodeIdentity folderIdentity,
        IEnumerable<LinkId> nodeIds,
        CancellationToken cancellationToken)
    {
        var parameters = new MultipleLinkActionParameters { LinkIds = nodeIds.Select(x => x.Value) };

        await client.TrashChildrenAsync(folderIdentity.ShareId, folderIdentity.NodeId, parameters, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task DeleteFolderChildrenAsync(
        FoldersApiClient client,
        INodeIdentity folderIdentity,
        IEnumerable<LinkId> nodeIds,
        CancellationToken cancellationToken)
    {
        var parameters = new MultipleLinkActionParameters { LinkIds = nodeIds.Select(x => x.Value) };

        await client.DeleteChildrenAsync(folderIdentity.ShareId, folderIdentity.NodeId, parameters, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<FolderNode> CreateFolderAsync(
        ProtonDriveClient client,
        IShareForCommand share,
        INodeIdentity parentFolder,
        string name,
        CancellationToken cancellationToken)
    {
        var newFolderNodeIdentity = new NodeIdentity { ShareId = share.ShareId, VolumeId = parentFolder.VolumeId, NodeId = parentFolder.NodeId };

        using var parentFolderKey = await Node.GetKeyAsync(client, newFolderNodeIdentity, cancellationToken).ConfigureAwait(false);
        using var signingKey = await client.Account.GetAddressPrimaryKeyAsync(new AddressId(share.MembershipAddressId), cancellationToken).ConfigureAwait(false);
        var parentHashKey = await Node.GetHashKeyAsync(client, newFolderNodeIdentity, cancellationToken).ConfigureAwait(false);

        var hashKey = RandomNumberGenerator.GetBytes(32);

        Node.GetCommonCreationParameters(
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
            ParentLinkId = parentFolder.NodeId.Value,
            Passphrase = encryptedKeyPassphrase,
            PassphraseSignature = keyPassphraseSignature,
            SignatureEmailAddress = share.MembershipEmailAddress,
            Key = armoredKey,
            HashKey = key.EncryptAndSign(hashKey, key),
        };

        var response = await client.FoldersApi.CreateFolderAsync(share.ShareId, parameters, cancellationToken).ConfigureAwait(false);

        var folderId = new LinkId(response.FolderId.Value);

        client.SecretsCache.Set(Node.GetNodeKeyCacheKey(parentFolder.VolumeId, folderId), key.ToBytes());
        client.SecretsCache.Set(Node.GetNameSessionKeyCacheKey(parentFolder.VolumeId, folderId), nameSessionKey.Export().Token);
        client.SecretsCache.Set(Node.GetPassphraseSessionKeyCacheKey(parentFolder.VolumeId, folderId), passphraseSessionKey.Export().Token);
        client.SecretsCache.Set(Node.GetHashKeyCacheKey(parentFolder.VolumeId, folderId), hashKey);

        return new FolderNode
        {
            NodeIdentity = new NodeIdentity
            {
                VolumeId = parentFolder.VolumeId,
                NodeId = folderId,
            },
            ParentId = parentFolder.NodeId,
            Name = name,
            NameHashDigest = ByteString.CopyFrom(nameHashDigest.ToArray()),
            State = NodeState.Active,
        };
    }
}
