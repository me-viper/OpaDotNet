namespace OpaDotNet.Wasm;

public partial class DefaultOpaImportsAbi
{
    private Guid NewGuid(string key)
    {
        var cacheKey = $"uuid.rfc4122.{key}";
        
        if (ValueCache.TryGetValue(cacheKey, out var result))
            return (Guid)result;
        
        result = Guid.NewGuid();
        ValueCache.TryAdd(cacheKey, result);

        return (Guid)result;
    }
}