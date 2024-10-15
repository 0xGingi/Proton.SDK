using System.Diagnostics.CodeAnalysis;

namespace Proton.Sdk.Cryptography;

public delegate TResult SecretTransform<in TState, out TResult>(TState state, ReadOnlySpan<byte> secretBytes, byte flags);
public delegate TResult SecretTransform<out TResult>(ReadOnlySpan<byte> secretBytes, byte flags);

public interface ISecretsCache
{
    void Set(CacheKey cacheKey, ReadOnlySpan<byte> secretBytes, byte flags, TimeSpan expiration);

    void IncludeInGroup(CacheKey groupCacheKey, ReadOnlySpan<CacheKey> memberCacheKeys);

    bool TryUse<TState, TResult>(CacheKey cacheKey, TState state, SecretTransform<TState, TResult> transform, [MaybeNullWhen(false)] out TResult result)
        where TResult : notnull;

    bool TryUseGroup<TState, TResult>(
        CacheKey groupCacheKey,
        TState state,
        SecretTransform<TState, TResult> transform,
        [MaybeNullWhen(false)] out List<TResult> result)
        where TResult : notnull;

    void Remove(CacheKey cacheKey);
}
