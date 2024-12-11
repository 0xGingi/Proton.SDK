using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Proton.Sdk.Cryptography;

namespace Proton.Sdk.CExports;

internal class InteropFallbackSecretsCacheDecorator(ISecretsCache decoratedInstance, Func<CacheKey, bool> onSecretRequested, ILoggerFactory? loggerFactory) : ISecretsCache
{
    private readonly ISecretsCache _decoratedInstance = decoratedInstance;
    private readonly Func<CacheKey, bool> _onSecretRequested = onSecretRequested;
    private readonly ILogger _logger = loggerFactory is not null ? loggerFactory.CreateLogger<InteropFallbackSecretsCacheDecorator>() : NullLogger.Instance;

    public void Set(CacheKey cacheKey, ReadOnlySpan<byte> secretBytes, byte flags, TimeSpan expiration)
    {
        _decoratedInstance.Set(cacheKey, secretBytes, flags, expiration);
    }

    public void IncludeInGroup(CacheKey groupCacheKey, ReadOnlySpan<CacheKey> memberCacheKeys)
    {
        _decoratedInstance.IncludeInGroup(groupCacheKey, memberCacheKeys);
    }

    public bool TryUse<TState, TResult>(CacheKey cacheKey, TState state, SecretTransform<TState, TResult> transform, [MaybeNullWhen(false)] out TResult result)
        where TResult : notnull
    {
        if (!_decoratedInstance.TryUse(cacheKey, state, transform, out result))
        {
            var secretAdded = _onSecretRequested.Invoke(cacheKey);
            if (!secretAdded || !_decoratedInstance.TryUse(cacheKey, state, transform, out result))
            {
                _logger.LogWarning($"Key cache miss for {cacheKey}. It needs to be fetched from the API.");
                return false;
            }
        }

        return true;
    }

    public bool TryUseGroup<TState, TResult>(
        CacheKey groupCacheKey,
        TState state,
        SecretTransform<TState, TResult> transform,
        [MaybeNullWhen(false)] out List<TResult> result)
        where TResult : notnull
    {
        return _decoratedInstance.TryUseGroup(groupCacheKey, state, transform, out result);
    }

    public void Remove(CacheKey cacheKey)
    {
        _decoratedInstance.Remove(cacheKey);
    }
}
