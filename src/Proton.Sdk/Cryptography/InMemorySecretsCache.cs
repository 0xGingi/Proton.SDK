using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Proton.Sdk.Cryptography;

public sealed class InMemorySecretsCache(ILogger<InMemorySecretsCache>? logger = null) : ISecretsCache
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly ILogger<InMemorySecretsCache> _logger = logger ?? new NullLogger<InMemorySecretsCache>();

    public void Set(CacheKey cacheKey, ReadOnlySpan<byte> secretBytes, byte flags, TimeSpan expiration)
    {
        lock (_memoryCache)
        {
            using var entry = _memoryCache.CreateEntry(cacheKey);

            if (expiration != Timeout.InfiniteTimeSpan)
            {
                entry.AbsoluteExpirationRelativeToNow = expiration;
            }

            entry.Value = new Secret(secretBytes.ToArray(), flags);
            _logger.LogDebug("Set {ValueLength}-byte value for key {CacheKey}", secretBytes.Length, cacheKey);
        }
    }

    public void IncludeInGroup(CacheKey groupCacheKey, ReadOnlySpan<CacheKey> memberCacheKeys)
    {
        lock (_memoryCache)
        {
            using var entry = _memoryCache.CreateEntry(groupCacheKey);

            entry.Value = memberCacheKeys.ToArray();
        }
    }

    public bool TryUse<TState, TResult>(
        CacheKey cacheKey,
        TState state,
        SecretTransform<TState, TResult> transform,
        [MaybeNullWhen(false)] out TResult result)
        where TResult : notnull
    {
        Secret? secret;

        lock (_memoryCache)
        {
            if (!_memoryCache.TryGetValue(cacheKey, out secret) || secret is null)
            {
                _logger.LogDebug("Key {CacheKey} not found", cacheKey);

                result = default;
                return false;
            }

            _logger.LogDebug("Found {ValueLength}-byte value for {CacheKey}", secret.Bytes.Length, cacheKey);
        }

        result = transform.Invoke(state, secret.Bytes, secret.Flags);

        return true;
    }

    public bool TryUseGroup<TState, TResult>(
        CacheKey groupCacheKey,
        TState state,
        SecretTransform<TState, TResult> transform,
        [MaybeNullWhen(false)] out List<TResult> result)
        where TResult : notnull
    {
        lock (_memoryCache)
        {
            if (!_memoryCache.TryGetValue<CacheKey[]>(groupCacheKey, out var cacheKeys) || cacheKeys is null)
            {
                _logger.LogDebug("Group key {GroupCacheKey} not found", groupCacheKey);

                result = null;
                return false;
            }

            _logger.LogDebug("Found {Count} cache keys for {GroupCacheKey}", cacheKeys.Length, groupCacheKey);

            result = TransformEntries(cacheKeys, state, transform).ToList();

            return true;
        }
    }

    public void Remove(CacheKey cacheKey)
    {
        lock (_memoryCache)
        {
            _memoryCache.Remove(cacheKey);
            _logger.LogDebug("Removed entry for key {CacheKey}", cacheKey);
        }
    }

    private IEnumerable<TResult> TransformEntries<TResult, TState>(CacheKey[] cacheKeys, TState state, SecretTransform<TState, TResult> transform)
        where TResult : notnull
    {
        foreach (var cacheKey in cacheKeys)
        {
            if (!TryUse(cacheKey, state, transform, out var transformedSecret))
            {
                continue;
            }

            yield return transformedSecret;
        }
    }

    private sealed class Secret(byte[] bytes, byte flags)
    {
        public byte[] Bytes { get; } = bytes;
        public byte Flags { get; } = flags;
    }
}
