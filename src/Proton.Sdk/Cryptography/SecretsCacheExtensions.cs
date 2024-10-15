using System.Diagnostics.CodeAnalysis;

namespace Proton.Sdk.Cryptography;

public static class SecretsCacheExtensions
{
    public static void Set(this ISecretsCache secretsCache, CacheKey cacheKey, ReadOnlySpan<byte> secretBytes, byte flags = 0)
    {
        secretsCache.Set(cacheKey, secretBytes, flags, Timeout.InfiniteTimeSpan);
    }

    public static bool TryUse<TResult>(
        this ISecretsCache secretsCache,
        CacheKey cacheKey,
        SecretTransform<TResult> transform,
        [MaybeNullWhen(false)] out TResult result)
        where TResult : notnull
    {
        return secretsCache.TryUse(cacheKey, transform, (t, bytes, flags) => t.Invoke(bytes, flags), out result);
    }

    public static bool TryUseGroup<TResult>(
        this ISecretsCache secretsCache,
        CacheKey groupCacheKey,
        SecretTransform<TResult> transform,
        [MaybeNullWhen(false)] out List<TResult> result)
        where TResult : notnull
    {
        return secretsCache.TryUseGroup(groupCacheKey, transform, (t, bytes, flags) => t.Invoke(bytes, flags), out result);
    }
}
