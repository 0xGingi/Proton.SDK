namespace Proton.Sdk.CExports;

public static class CacheKeyExtensions
{
    public static KeyCacheMissMessage ToCacheMissMessage(this CacheKey cacheKey)
    {
        if (cacheKey.Context is null)
        {
            return new KeyCacheMissMessage
            {
                HolderId = cacheKey.ValueHolderId,
                HolderName = cacheKey.ValueHolderName,
                ValueName = cacheKey.ValueName,
            };
        }

        return new KeyCacheMissMessage
        {
            HolderId = cacheKey.ValueHolderId,
            HolderName = cacheKey.ValueHolderName,
            ContextId = cacheKey.Context.Value.Id,
            ContextName = cacheKey.Context.Value.Name,
            ValueName = cacheKey.ValueName,
        };
    }
}
