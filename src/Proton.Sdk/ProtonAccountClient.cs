using System.Runtime.InteropServices;
using Proton.Cryptography.Pgp;
using Proton.Sdk.Addresses;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Events;
using Proton.Sdk.Keys;
using Proton.Sdk.Users;

namespace Proton.Sdk;

public sealed class ProtonAccountClient(ProtonApiSession session)
{
    private const string CacheUserValueHolderName = "user";
    private const string CacheUserKeysValueName = "keys";
    private const string CacheUserKeyValueHolderName = "user-key";
    private const string CacheUserKeyDataValueName = "data";

    private readonly HttpClient _httpClient = session.GetHttpClient();
    private readonly UserId _userId = session.UserId;

    internal UsersApiClient UsersApi => new(_httpClient);
    internal AddressesApiClient AddressesApi => new(_httpClient);
    internal KeysApiClient KeysApi => new(_httpClient);
    internal EventsApiClient EventsApi => new(_httpClient);

    internal ISecretsCache SecretsCache { get; } = session.SecretsCache;

    public Task<Address> GetAddressAsync(AddressId addressId, CancellationToken cancellationToken)
    {
        return Address.GetAsync(this, addressId, cancellationToken);
    }

    public Task<List<Address>> GetAddressesAsync(CancellationToken cancellationToken)
    {
        return Address.GetAllAsync(this, cancellationToken);
    }

    public Task<Address> GetDefaultAddressAsync(CancellationToken cancellationToken)
    {
        return Address.GetDefaultAsync(this, cancellationToken);
    }

    internal async Task<IReadOnlyList<PgpPrivateKey>> GetUserKeysAsync(CancellationToken cancellationToken)
    {
        if (!SecretsCache.TryUseGroup(GetUserKeyGroupCacheKey(_userId), (bytes, _) => PgpPrivateKey.Import(bytes), out var keys))
        {
            await RefreshUserKeysAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!SecretsCache.TryUseGroup(GetUserKeyGroupCacheKey(_userId), (bytes, _) => PgpPrivateKey.Import(bytes), out keys))
        {
            throw new ProtonApiException("No active user key was found.");
        }

        return keys.ToList().AsReadOnly();
    }

    internal Task<IReadOnlyList<PgpPrivateKey>> GetAddressKeysAsync(AddressId addressId, CancellationToken cancellationToken)
    {
        return Address.GetKeysAsync(this, addressId, cancellationToken);
    }

    internal Task<PgpPrivateKey> GetAddressPrimaryKeyAsync(AddressId addressId, CancellationToken cancellationToken)
    {
        return Address.GetPrimaryKeyAsync(this, addressId, cancellationToken);
    }

    internal async Task<IReadOnlyList<PgpPublicKey>> GetAddressPublicKeysAsync(string emailAddress, CancellationToken cancellationToken)
    {
        var publicKeysResponse = await KeysApi.GetActivePublicKeysAsync(emailAddress, cancellationToken).ConfigureAwait(false);

        var publicKeys = new List<PgpPublicKey>(publicKeysResponse.Address.Keys.Count);

        publicKeys.AddRange(
            publicKeysResponse.Address.Keys
                .Where(keyEntry => keyEntry.Flags.HasFlag(PublicKeyFlags.IsNotCompromised))
                .Select(entry => PgpPublicKey.Import(entry.PublicKey)));

        return publicKeys;
    }

    private static CacheKey GetUserKeyGroupCacheKey(UserId id) => new(CacheUserValueHolderName, id.Value, CacheUserKeysValueName);
    private static CacheKey GetUserKeyCacheKey(UserKeyId id) => new(CacheUserKeyValueHolderName, id.Value, CacheUserKeyDataValueName);

    private async Task RefreshUserKeysAsync(CancellationToken cancellationToken)
    {
        var response = await UsersApi.GetAuthenticatedUserAsync(cancellationToken).ConfigureAwait(false);

        var unlockedKeys = new List<PgpPrivateKey>(response.User.Keys.Count);
        var cacheKeys = new List<CacheKey>(response.User.Keys.Count);

        foreach (var userKey in response.User.Keys)
        {
            var userKeyId = new UserKeyId(userKey.Id);

            if (!userKey.IsActive)
            {
                continue;
            }

            if (!SecretsCache.TryUse(
                ProtonApiSession.GetUserKeyPassphraseCacheKey(userKeyId),
                (passphrase, _) => PgpPrivateKey.ImportAndUnlock(userKey.PrivateKey.Bytes.Span, passphrase),
                out var unlockedUserKey))
            {
                // TODO: do something about that
                continue;
            }

            var cacheKey = GetUserKeyCacheKey(userKeyId);
            SecretsCache.Set(cacheKey, unlockedUserKey.ToBytes(), userKey.IsPrimary ? (byte)1 : (byte)0);

            unlockedKeys.Add(unlockedUserKey);
            cacheKeys.Add(cacheKey);
        }

        if (unlockedKeys.Count == 0)
        {
            throw new ProtonApiException("No active user key was found.");
        }

        SecretsCache.IncludeInGroup(GetUserKeyGroupCacheKey(new UserId(response.User.Id)), CollectionsMarshal.AsSpan(cacheKeys));
    }
}
