using Proton.Cryptography.Pgp;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Drive.Links;
using Proton.Sdk.Drive.Shares;

namespace Proton.Sdk.Drive;

public sealed class Share(ShareId id, VolumeId volumeId, LinkId rootNodeId, AddressId membershipAddressId, string membershipEmailAddress) : IShareForCommand
{
    private const string CacheValueHolderName = "drive.share";
    private const string CacheShareKeyValueName = "key";

    public VolumeId VolumeId { get; } = volumeId;

    public ShareId Id { get; } = id;

    public LinkId RootNodeId { get; } = rootNodeId;

    public AddressId MembershipAddressId { get; } = membershipAddressId;

    public string MembershipEmailAddress { get; } = membershipEmailAddress;

    internal static async Task<Share> GetAsync(ProtonDriveClient client, ShareId id, CancellationToken cancellationToken)
    {
        var response = await client.SharesApi.GetShareAsync(id, cancellationToken).ConfigureAwait(false);

        var addressId = new AddressId(response.AddressId);
        var addressKeys = await client.Account.GetAddressKeysAsync(addressId, cancellationToken).ConfigureAwait(false);

        var passphrase = new PgpPrivateKeyRing(addressKeys).Decrypt(response.Passphrase);

        var key = PgpPrivateKey.ImportAndUnlock(response.Key, passphrase);
        client.SecretsCache.Set(GetShareKeyCacheKey(id), key.ToBytes());

        var address = await client.Account.GetAddressAsync(new AddressId(response.AddressId), cancellationToken).ConfigureAwait(false);

        return new Share(id, new VolumeId(response.VolumeId), new LinkId(response.RootLinkId), address.Id, address.EmailAddress);
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
