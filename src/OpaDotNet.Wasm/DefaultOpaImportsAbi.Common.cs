using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Web;

using Microsoft.Extensions.Primitives;

using OpaDotNet.Wasm.Rego;

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

    private static byte[] Base64UrlDecode(string x)
    {
        var s = x.PadRight(x.Length + (4 - x.Length % 4) % 4, '=')
            .Replace('-', '+')
            .Replace('_', '/');

        return Convert.FromBase64String(s);
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

    private static string UrlQueryDecode(string x)
    {
        return HttpUtility.UrlDecode(x);
    }

    private static Dictionary<string, List<string>>? UrlQueryDecodeObject(string x)
    {
        if (string.IsNullOrWhiteSpace(x))
            return null;

        var result = new Dictionary<string, List<string>>();

        var queryIndex = x.IndexOf('?');
        queryIndex = queryIndex == -1 ? 0 : queryIndex;

        var q = x[queryIndex..];

        var queryParts = new StringTokenizer(q, new[] { '&' });

        foreach (var part in queryParts)
        {
            var eqIndex = part.IndexOf('=');

            string key;
            string? value = null;

            if (eqIndex == -1)
                key = part.ToString();
            else
            {
                key = part.Substring(0, eqIndex);
                value = HttpUtility.UrlDecode(part.Substring(eqIndex + 1));
            }

            if (!result.ContainsKey(key))
                result.Add(key, new());

            if (!string.IsNullOrWhiteSpace(value))
                result[key].Add(value);
        }

        return result;
    }

    private static string UrlQueryEncode(string x)
    {
        return HttpUtility.UrlEncode(x);
    }

    private static string? UrlQueryEncodeObject(JsonNode? obj, JsonSerializerOptions options)
    {
        var result = new List<string>();

        if (obj == null)
            return null;

        // Empty object will be represented as {} which will be converted to set.
        // But we know set is not allowed here so it must be empty object.
        if (obj.IsRegoSet())
            return string.Empty;

        if (obj is not JsonObject jo)
            return null;

        foreach (var (k, v) in jo)
        {
            if (v.IsRegoSet())
            {
                if (!v.TryGetRegoSet<string>(out var set, options))
                    return null;

                result.Add(string.Join('&', set.Set.Select(p => $"{k}={HttpUtility.UrlEncode(p)}")));
                continue;
            }

            if (v is JsonArray ja)
            {
                var values = ja.Select(p => $"{k}={HttpUtility.UrlEncode(p!.GetValue<string>())}");
                result.Add(string.Join('&', values));
                continue;
            }

            result.Add($"{k}={HttpUtility.UrlEncode(v!.GetValue<string>())}");
        }

        return string.Join('&', result);
    }

    private static IEnumerable<string> RegexFindN(string pattern, string value, int number)
    {
        if (number == 0)
            return Array.Empty<string>();

        var regex = new Regex(pattern);
        var matches = regex.Matches(value);

        var result = number > 0
            ? matches.Take(number).Select(p => p.Value)
            : matches.Select(p => p.Value);

        return result;
    }

    private static string RegexReplace(string s, string pattern, string value)
    {
        return Regex.Replace(s, pattern, value);
    }

    private static IEnumerable<string> RegexSplit(string pattern, string value)
    {
        return Regex.Split(value, pattern);
    }
}