using Proton.Cryptography.Pgp;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Drive.Links;
using Proton.Sdk.Drive.Shares;

namespace Proton.Sdk.Drive;

public sealed partial class Share : IShare
{
    private const string CacheValueHolderName = "drive.share";
    private const string CacheShareKeyValueName = "key";

    public ShareMetadata Metadata()
    {
        return new ShareMetadata
        {
            ShareId = ShareId,
            MembershipEmailAddress = MembershipEmailAddress,
            MembershipAddressId = MembershipAddressId,
        };
    }

    internal static async Task<Share> GetAsync(ProtonDriveClient client, ShareId shareId, CancellationToken cancellationToken)
    {
        var fetchedShare = await client.SharesApi.GetShareAsync(shareId, cancellationToken).ConfigureAwait(false);

        var addressId = new AddressId(fetchedShare.AddressId);
        var addressKeys = await client.Account.GetAddressKeysAsync(addressId, cancellationToken).ConfigureAwait(false);

        var passphrase = new PgpPrivateKeyRing(addressKeys).Decrypt(fetchedShare.Passphrase);

        var key = PgpPrivateKey.ImportAndUnlock(fetchedShare.Key, passphrase);
        client.SecretsCache.Set(GetShareKeyCacheKey(shareId), key.ToBytes());

        var fetchedAddress = await client.Account.GetAddressAsync(addressId, cancellationToken).ConfigureAwait(false);

        return new Share
        {
            ShareId = shareId,
            VolumeId = new VolumeId(fetchedShare.VolumeId),
            RootNodeId = new LinkId(fetchedShare.RootLinkId),
            MembershipAddressId = fetchedAddress.Id,
            MembershipEmailAddress = fetchedAddress.EmailAddress,
        };
    }

    internal static async Task DeleteFromTrashAsync(SharesApiClient client, ShareId shareId, IEnumerable<LinkId> nodeIds, CancellationToken cancellationToken)
    {
        var parameters = new MultipleLinkActionParameters { LinkIds = nodeIds.Select(x => x.Value) };

        await client.DeleteFromTrashAsync(shareId, parameters, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<PgpPrivateKey> GetKeyAsync(ProtonDriveClient client, ShareId id, CancellationToken cancellationToken)
    {
        if (!client.SecretsCache.TryUse(GetShareKeyCacheKey(id), (bytes, _) => PgpPrivateKey.Import(bytes), out var key))
        {
            await GetAsync(client, id, cancellationToken).ConfigureAwait(false);

            if (!client.SecretsCache.TryUse(GetShareKeyCacheKey(id), (bytes, _) => PgpPrivateKey.Import(bytes), out key))
            {
                throw new ProtonApiException($"Could not get passphrase session key for {id}");
            }
        }

        return key;
    }

    private static CacheKey GetShareKeyCacheKey(ShareId shareId) => new(CacheValueHolderName, shareId.Value, CacheShareKeyValueName);
}
