using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace OpaDotNet.Wasm;

public partial class DefaultOpaImportsAbi
{
    private static Random _random = new();
    
    private int RandIntN(string key, int n)
    {
        var cacheKey = $"rand.intn.{key}";
        
        if (ValueCache.TryGetValue(cacheKey, out var result))
            return (int)result;
        
        result = _random.Next(n);
        ValueCache.TryAdd(cacheKey, result);
        
        return (int)result;
    }
    
    private Guid NewGuid(string key)
    {
        var cacheKey = $"uuid.rfc4122.{key}";
        
        if (ValueCache.TryGetValue(cacheKey, out var result))
            return (Guid)result;
        
        result = Guid.NewGuid();
        ValueCache.TryAdd(cacheKey, result);

        return (Guid)result;
    }
    
    private class OpaSet
    {
        public static bool TryParse<T>(JsonArray ar, [MaybeNullWhen(false)] out Tuple<T> tuple)
        {
            tuple = null;
            
            if (ar.Count < 1)
                return false;
            
            if (ar[0] is not JsonValue)
                return false;
            
            var val = ar[0]!.GetValue<T>();
            tuple = Tuple.Create(val);
            
            return true;
        }
        
        public static bool TryParse<T1, T2>(JsonArray ar, [MaybeNullWhen(false)] out Tuple<T1, T2> tuple)
        {
            tuple = null;
            
            if (ar.Count < 1)
                return false;
            
            if (ar[0] is not JsonValue || ar[1] is not JsonValue)
                return false;
            
            var val1 = ar[0]!.GetValue<T1>();
            var val2 = ar[1]!.GetValue<T2>();
            
            tuple = Tuple.Create(val1, val2);
            
            return true;
        }
    }
}