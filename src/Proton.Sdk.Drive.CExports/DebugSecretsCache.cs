using Proton.Sdk.Cryptography;

namespace Proton.Sdk.Drive.CExports;

/// <summary>
/// Class used to debug the secrets cache and to figure out what keys are missing.
/// </summary>
internal class DebugSecretsCache : ISecretsCache
{
    private readonly ISecretsCache _inner;

    public DebugSecretsCache(ISecretsCache inner)
    {
        _inner = inner;
    }

    public void Set(CacheKey key, ReadOnlySpan<byte> value, byte flags, TimeSpan expiration)
    {
        _inner.Set(key, value, flags, expiration);
    }

    public void IncludeInGroup(CacheKey groupKey, ReadOnlySpan<CacheKey> keys)
    {
        _inner.IncludeInGroup(groupKey, keys);
    }

    public bool TryUse<TState, TResult>(CacheKey key, TState state, SecretTransform<TState, TResult> transform, out TResult result) where TResult : notnull
    {
        result = default!;
        var found = _inner.TryUse(key, state, transform, out var result_1);
        if (found && result_1 is not null)
        {
            result = result_1;
        }
        else if (!found)
        {
            Console.WriteLine($"[DebugSecretsCache] Cache miss: {key}");
        }

        return found;
    }

    public bool TryUseGroup<TState, TResult>(CacheKey groupKey, TState state, SecretTransform<TState, TResult> transform, out List<TResult> result) where TResult : notnull
    {
        result = default!;
        var found = _inner.TryUseGroup(groupKey, state, transform, out var result_1);
        if (found && result_1 is not null)
        {
            result = result_1;
        }
        else if (!found)
        {
            Console.WriteLine($"[DebugSecretsCache] Group cache miss: {groupKey}");
        }

        return found;
    }

    public void Remove(CacheKey cacheKey)
    {
        _inner.Remove(cacheKey);
    }
}
