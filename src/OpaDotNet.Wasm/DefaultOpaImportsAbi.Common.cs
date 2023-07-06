using System.Text;

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
    
    private static string Base64UrlEncodeNoPad(string x)
    {
        var bytes = Encoding.UTF8.GetBytes(x);
        
        return Convert.ToBase64String(bytes)
            .Trim('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
    
    private static string HexEncode(string x)
    {
        return Convert.ToHexString(Encoding.UTF8.GetBytes(x)).ToLowerInvariant();
    }
    
    private static string HexDecode(string x)
    {
        var bytes = Convert.FromHexString(x);
        return Encoding.UTF8.GetString(bytes);
    }
}