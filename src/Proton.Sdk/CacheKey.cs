namespace Proton.Sdk;

public readonly record struct CacheKey(CacheKeyContext? Context, string ValueHolderName, string ValueHolderId, string ValueName)
{
    public CacheKey(string contextName, string contextId, string valueHolderName, string valueHolderId, string valueName)
        : this(new CacheKeyContext(contextName, contextId), valueHolderName, valueHolderId, valueName)
    {
    }

    public CacheKey(string valueHolderName, string valueHolderId, string valueName)
        : this(null, valueHolderName, valueHolderId, valueName)
    {
    }

    public override string ToString()
    {
        return Context is not { } context
            ? $"{ValueHolderName}:{ValueHolderId}:{ValueName}"
            : $"{context.Name}:{context.Id}:{ValueHolderName}:{ValueHolderId}:{ValueName}";
    }
}
