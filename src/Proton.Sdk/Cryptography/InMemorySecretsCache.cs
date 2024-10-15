using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;

namespace Proton.Sdk.Cryptography;

public sealed class InMemorySecretsCache : ISecretsCache
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());

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
                result = default;
                return false;
            }
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
                Debug.WriteLine($"Cache: Group key {groupCacheKey} not found");
                result = default;
                return false;
            }

            Debug.WriteLine($"Cache: Found {cacheKeys.Length} cache keys for {groupCacheKey}");

            result = TransformEntries(cacheKeys, state, transform).ToList();

            return true;
        }
    }

    public void Remove(CacheKey cacheKey)
    {
        lock (_memoryCache)
        {
            _memoryCache.Remove(cacheKey);
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
