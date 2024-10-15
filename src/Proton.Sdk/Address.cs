using CommunityToolkit.HighPerformance;
using Proton.Cryptography.Pgp;
using Proton.Sdk.Addresses;
using Proton.Sdk.Cryptography;

namespace Proton.Sdk;

public sealed class Address(AddressId id, int order, string emailAddress, AddressStatus status, IReadOnlyList<AddressKey> keys, int primaryKeyIndex)
{
    private const string CacheAddressValueHolderName = "address";
    private const string CacheAddressKeysValueName = "keys";
    private const string CacheAddressKeyValueHolderName = "address-key";
    private const string CacheAddressKeyDataValueName = "data";

    public AddressId Id { get; } = id;
    public int Order { get; } = order;
    public string EmailAddress { get; } = emailAddress;
    public AddressStatus Status { get; } = status;
    public IReadOnlyList<AddressKey> Keys { get; } = keys;
    public int PrimaryKeyIndex { get; } = primaryKeyIndex;

    public AddressKey PrimaryKey => Keys[PrimaryKeyIndex];

    internal static async Task<List<Address>> GetAllAsync(ProtonAccountClient client, CancellationToken cancellationToken)
    {
        var addressListResponse = await client.AddressesApi.GetAddressesAsync(cancellationToken).ConfigureAwait(false);

        var addresses = new List<Address>(addressListResponse.Addresses.Count);

        var userKeys = await client.GetUserKeysAsync(cancellationToken).ConfigureAwait(false);

        foreach (var dto in addressListResponse.Addresses)
        {
            try
            {
                addresses.Add(FromDto(dto, userKeys, client.SecretsCache));
            }
            catch
            {
                // TODO: log that
                continue;
            }
        }

        return addresses;
    }

    internal static async Task<Address> GetAsync(ProtonAccountClient client, AddressId addressId, CancellationToken cancellationToken)
    {
        var userKeys = await client.GetUserKeysAsync(cancellationToken).ConfigureAwait(false);

        var response = await client.AddressesApi.GetAddressAsync(addressId, cancellationToken).ConfigureAwait(false);

        return FromDto(response.Address, userKeys, client.SecretsCache);
    }

    internal static async Task<Address> GetDefaultAsync(ProtonAccountClient client, CancellationToken cancellationToken)
    {
        var addresses = await GetAllAsync(client, cancellationToken).ConfigureAwait(false);

        if (addresses.Count == 0)
        {
            throw new ProtonApiException("User has no address");
        }

        addresses.Sort((a, b) => a.Order.CompareTo(b.Order));

        return addresses[0];
    }

    internal static Address FromDto(AddressDto dto, IReadOnlyList<PgpPrivateKey> userKeys, ISecretsCache secretsCache)
    {
        int? primaryKeyIndex = null;

        var id = new AddressId(dto.Id);

        var keys = new List<AddressKey>(dto.Keys.Count);
        var addressKeyCacheKeys = new CacheKey[dto.Keys.Count];
        var keyIndex = 0;

        foreach (var keyDto in dto.Keys)
        {
            if (!keyDto.IsActive)
            {
                continue;
            }

            var keyId = new AddressKeyId(keyDto.Id);

            try
            {
                PgpPrivateKey unlockedKey;

                if (keyDto is { Token: not null, Signature: not null })
                {
                    var passphrase = GetAddressKeyTokenPassphrase(keyDto.Token.Value, keyDto.Signature.Value, userKeys);
                    unlockedKey = PgpPrivateKey.ImportAndUnlock(keyDto.PrivateKey, passphrase.Span);
                }
                else if (!secretsCache.TryUse(
                    ProtonApiSession.GetLegacyAddressKeyPassphraseCacheKey(keyId),
                    (passphrase, _) => PgpPrivateKey.ImportAndUnlock(keyDto.PrivateKey, passphrase),
                    out unlockedKey))
                {
                    // TODO: log that
                    continue;
                }

                var addressKeyCacheKey = GetAddressKeyCacheKey(keyId);
                secretsCache.Set(addressKeyCacheKey, unlockedKey.ToBytes(), keyDto.IsPrimary ? (byte)1 : (byte)0);
                addressKeyCacheKeys[keyIndex] = addressKeyCacheKey;
            }
            catch
            {
                // TODO: log that
                continue;
            }

            var key = new AddressKey(id, keyId, (keyDto.Flags & AddressKeyFlags.IsAllowedForEncryption) > 0);

            keys.Add(key);

            if (keyDto.IsPrimary)
            {
                primaryKeyIndex = keyIndex;
            }

            ++keyIndex;
        }

        if (primaryKeyIndex is null)
        {
            throw new ProtonApiException($"Address {dto.Id} has no primary key");
        }

        secretsCache.IncludeInGroup(GetAddressKeyGroupCacheKey(id), addressKeyCacheKeys.AsSpan()[..keys.Count]);

        return new Address(id, dto.Order, dto.Email, dto.Status, keys.AsReadOnly(), primaryKeyIndex.Value);
    }

    internal static async Task<IReadOnlyList<PgpPrivateKey>> GetKeysAsync(
        ProtonAccountClient client,
        AddressId addressId,
        CancellationToken cancellationToken)
    {
        var keys = await EnumerateAddressKeysAsync(client, addressId, cancellationToken).ConfigureAwait(false);

        return keys.Select(x => x.PrivateKey).ToList().AsReadOnly();
    }

    internal static async Task<PgpPrivateKey> GetPrimaryKeyAsync(ProtonAccountClient client, AddressId addressId, CancellationToken cancellationToken)
    {
        var addressKeys = await EnumerateAddressKeysAsync(client, addressId, cancellationToken).ConfigureAwait(false);

        return addressKeys.Where(x => x.IsPrimary).Select(x => x.PrivateKey).First();
    }

    private static CacheKey GetAddressKeyGroupCacheKey(AddressId id) => new(CacheAddressValueHolderName, id.Value, CacheAddressKeysValueName);
    private static CacheKey GetAddressKeyCacheKey(AddressKeyId id) => new(CacheAddressKeyValueHolderName, id.Value, CacheAddressKeyDataValueName);

    private static ReadOnlyMemory<byte> GetAddressKeyTokenPassphrase(
        PgpArmoredMessage token,
        PgpArmoredSignature signature,
        IReadOnlyList<PgpPrivateKey> userKeys)
    {
        // TODO: verification
        using var decryptingStream = PgpDecryptingStream.Open(token.Bytes.AsStream(), new PgpPrivateKeyRing(userKeys));

        using var passphraseStream = new MemoryStream();
        decryptingStream.CopyTo(passphraseStream);

        // TODO: avoid another allocation
        return passphraseStream.ToArray();
    }

    private static async Task<IEnumerable<CachedAddressKey>> EnumerateAddressKeysAsync(
        ProtonAccountClient client,
        AddressId addressId,
        CancellationToken cancellationToken)
    {
        if (!client.SecretsCache.TryUseGroup(
            GetAddressKeyGroupCacheKey(addressId),
            (bytes, isPrimary) => new CachedAddressKey(PgpPrivateKey.Import(bytes), isPrimary == 1),
            out var keys))
        {
            await GetAsync(client, addressId, cancellationToken).ConfigureAwait(false);

            if (!client.SecretsCache.TryUseGroup(
                GetAddressKeyGroupCacheKey(addressId),
                (bytes, isPrimary) => new CachedAddressKey(PgpPrivateKey.Import(bytes), isPrimary == 1),
                out keys))
            {
                throw new ProtonApiException($"Could not get address keys for address {addressId}");
            }
        }

        return keys;
    }

    private readonly struct CachedAddressKey(PgpPrivateKey privateKey, bool isPrimary)
    {
        public PgpPrivateKey PrivateKey { get; } = privateKey;
        public bool IsPrimary { get; } = isPrimary;
    }
}
