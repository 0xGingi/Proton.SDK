namespace Proton.Sdk.CExports;

public static class CacheKeyExtensions
{
    public static KeyCacheMissMessage ToCacheMissMessage(this CacheKey cacheKey)
    {
        if (cacheKey.Context is null)
        {
            return new KeyCacheMissMessage
            {
                NodeId = cacheKey.ValueHolderId,
                KeyType = cacheKey.ValueName,
            };
        }

        return new KeyCacheMissMessage
        {
            NodeId = cacheKey.ValueHolderId,
            ContextId = cacheKey.Context.Value.Id,
            KeyType = cacheKey.ValueName,
        };
    }
}
