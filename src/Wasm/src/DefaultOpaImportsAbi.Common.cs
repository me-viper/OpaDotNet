using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;

using Json.More;

using Microsoft.Extensions.Primitives;

using OpaDotNet.Wasm.Rego;

using Semver;

using Yaml2JsonNode;

using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace OpaDotNet.Wasm;

public partial class DefaultOpaImportsAbi
{
    private static Random _random = new();

    private class RuntimeEnv
    {
        [UsedImplicitly]
        public string? Version { get; set; }

        [UsedImplicitly]
        public Dictionary<string, string> Env { get; set; } = new();
    }

    private static RuntimeEnv OpaRuntime()
    {
        var result = new RuntimeEnv();

        result.Version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
        var ev = Environment.GetEnvironmentVariables();

        foreach (var k in ev.Keys)
        {
            if (k?.ToString() == null || ev[k]?.ToString() == null)
                continue;

            result.Env.Add(k.ToString()!, ev[k]!.ToString()!);
        }

        return result;
    }

    /// <summary>
    /// When overriden allows replacing a pseudo-random number generator.
    /// </summary>
    /// <returns>A pseudo-random number generator.</returns>
    [ExcludeFromCodeCoverage]
    protected virtual Random Random()
    {
        return _random;
    }

    private int RandIntN(string key, int n) => CacheGetOrAddValue($"rand.intn.{key}", () => Random().Next(n));

    private Guid NewGuid(string key) => CacheGetOrAddValue($"uuid.rfc4122.{key}", NewGuid);

    internal record UuidParseResult(
        [property: JsonPropertyName("variant")]
        string Variant,
        [property: JsonPropertyName("version")]
        int Version
        );

    private UuidParseResult? UuidParse(ReadOnlySpan<char> uuid)
    {
        var index = 0;

        if (uuid.StartsWith("urn:uuid:"))
            index = "urn:uuid:".Length;

        if (!Guid.TryParse(uuid[index..], out var guid))
            return null;

        Span<byte> bytes = stackalloc byte[16];

        if (!guid.TryWriteBytes(bytes))
            return null;

        var ver = bytes[7] >> 4;
        string? type;

        if ((bytes[8] & 0xc0) == 0x80)
            type = "RFC4122";
        else if ((bytes[8] & 0xe0) == 0xc0)
            type = "Microsoft";
        else if ((bytes[8] & 0xe0) == 0xe0)
            type = "Future";
        else
            type = "Reserved";

        return new(type, ver);
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

    private static string? UrlQueryEncodeObject(JsonNode? obj)
    {
        var result = new List<string>();

        if (obj is not JsonObject jo)
            return null;

        foreach (var (k, v) in jo)
        {
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

    private static bool? RegexTemplateMatch(string template, string value, string delimiterStart, string delimiterEnd)
    {
        if (delimiterStart.Length != 1 || delimiterEnd.Length != 1)
            return null;

        var pattern = new StringBuilder(template.Length);

        var iterations = 0;
        var maxIterations = template.Length;
        var index = 0;

        while (index < template.Length)
        {
            if (iterations++ > maxIterations)
                return null;

            var patEnd = -1;

            if (template[index] == delimiterStart[0])
            {
                index++;
                var patStart = index;
                var depth = 0;

                while (index < template.Length)
                {
                    if (iterations++ > maxIterations)
                        return null;

                    if (template[index] == delimiterStart[0])
                        depth++;
                    else if (template[index] == delimiterEnd[0])
                    {
                        if (depth > 0)
                            depth--;
                        else
                        {
                            patEnd = index;
                            break;
                        }
                    }

                    index++;
                }

                if (patStart >= patEnd)
                    return null;

                pattern.Append(template[patStart..patEnd]);
                index++;
                continue;
            }

            pattern.Append(template[index]);
            index++;
        }

        return Regex.IsMatch(value, pattern.ToString());
    }

    private static int? SemverCompare(string a, string b)
    {
        if (!SemVersion.TryParse(a, SemVersionStyles.Strict, out var va))
            return null;

        if (!SemVersion.TryParse(b, SemVersionStyles.Strict, out var vb))
            return null;

        return va.ComparePrecedenceTo(vb);
    }

    private static bool SemverIsValid(JsonNode? vsn)
    {
        if (vsn is not JsonValue jv)
            return false;

        if (!jv.TryGetValue<string>(out var v))
            return false;

        return SemVersion.TryParse(v, SemVersionStyles.Strict, out _);
    }

    private static bool YamlIsValid(JsonNode? node)
    {
        try
        {
            if (node is not JsonValue jv)
                return false;

            if (!jv.TryGetValue<string>(out var yaml))
                return false;

            var deserializer = new DeserializerBuilder().Build();
            _ = deserializer.Deserialize<object>(yaml);

            return true;
        }
        catch (YamlException)
        {
            return false;
        }
    }

    private static string? YamlMarshal(JsonNode? node)
    {
        var yaml = node?.ToYamlNode();

        if (yaml == null)
            return null;

        var doc = new YamlDocument(yaml);
        var s = new YamlStream(doc);
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        s.Save(sw);

        return sw.ToString();
    }

    private static object? YamlUnmarshal(string yamlString)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(yamlString))
                return null;

            var deserializer = new DeserializerBuilder().Build();
            var result = deserializer.Deserialize(new StringReader(yamlString));
            return result?.ToJsonDocument();
        }
        catch (YamlException)
        {
            return null;
        }
    }

    private const ulong B = 1;
    private const ulong Kb = 1000 * B;
    private const ulong Mb = 1000 * Kb;
    private const ulong Gb = 1000 * Mb;
    private const ulong Tb = 1000 * Gb;
    private const ulong Pb = 1000 * Tb;
    private const ulong Eb = 1000 * Pb;

    private const ulong Bi = 1u << (10 * 0);
    private const ulong Ki = 1u << (10 * 1);
    private const ulong Mi = 1u << (10 * 2);
    private const ulong Gi = 1u << (10 * 3);
    private const ulong Ti = (ulong)1u << (10 * 4);
    private const ulong Pi = (ulong)1u << (10 * 5);
    private const ulong Ei = (ulong)1u << (10 * 6);

    private static decimal? UnitsParse(string x)
    {
        if (string.IsNullOrWhiteSpace(x))
            return null;

        const decimal milli = 0.001m;

        var num = double.NaN;
        var unit = string.Empty;

        x = x.Replace("\"", "");

        for (var i = x.Length - 1; i >= 0; i--)
        {
            if (char.IsLetter(x[i]))
                continue;

            if (!double.TryParse(x[.. (i + 1)], CultureInfo.InvariantCulture, out num))
                return null;

            if (i + 1 < x.Length)
            {
                unit = x[(i + 1) ..];
                unit = $"{unit[0]}{unit[1..].ToLowerInvariant()}";
            }

            break;
        }

        if (double.IsNaN(num))
            return null;

        decimal? n = unit switch
        {
            "m" => milli,
            "" => B,
            "k" or "K" => Kb,
            "ki" or "Ki" => Ki,
            "M" => Mb,
            "mi" or "Mi" => Mi,
            "g" or "G" => Gb,
            "gi" or "Gi" => Gi,
            "t" or "T" => Tb,
            "ti" or "Ti" => Ti,
            "p" or "P" => Pb,
            "pi" or "Pi" => Pi,
            "e" or "E" => Eb,
            "ei" or "Ei" => Ei,
            _ => null,
        };

        if (n == null)
            return null;

        return n.Value * (decimal)num;
    }

    private static ulong? UnitsParseBytes(string x)
    {
        if (string.IsNullOrWhiteSpace(x))
            return null;

        var num = double.NaN;
        var unit = string.Empty;

        x = x.Replace("\"", "");

        for (var i = x.Length - 1; i >= 0; i--)
        {
            if (char.IsLetter(x[i]))
                continue;

            if (!double.TryParse(x[.. (i + 1)], CultureInfo.InvariantCulture, out num))
                return null;

            if (i + 1 < x.Length)
                unit = x[(i + 1) ..].ToLowerInvariant();

            break;
        }

        if (double.IsNaN(num))
            return null;

        ulong? n = unit switch
        {
            "" => B,
            "kb" or "k" => Kb,
            "kib" or "ki" => Ki,
            "mb" or "m" => Mb,
            "mib" or "mi" => Mi,
            "gb" or "g" => Gb,
            "gib" or "gi" => Gi,
            "tb" or "t" => Tb,
            "tib" or "ti" => Ti,
            "pb" or "p" => Pb,
            "pib" or "pi" => Pi,
            "eb" or "e" => Eb,
            "eib" or "ei" => Ei,
            _ => null,
        };

        if (n == null)
            return null;

        var result = n.Value * (decimal)num;

        return decimal.ToUInt64(result);
    }

    private static int[]? NumbersRangeStep(int a, int b, int step)
    {
        if (step < 1)
            return null;

        if (a == b)
            return new[] { a };

        var result = new List<int>();

        if (a < b)
        {
            for (var i = a; i <= b; i += step)
                result.Add(i);
        }
        else
        {
            for (var i = a; i >= b; i -= step)
                result.Add(i);
        }

        return result.ToArray();
    }

    private static bool ObjectSubset(JsonNode? super, JsonNode? sub, bool throwIfIncompatible = true)
    {
        if (super == null && sub == null)
            return true;

        if (super == null || sub == null)
            return false;

        if (super is JsonValue supVal && sub is JsonValue subVal)
            return supVal.IsEquivalentTo(subVal);

        if (super is JsonObject supObj && sub is JsonObject subObj)
            return ObjectSubsetInner(supObj, subObj);

        if (super is JsonArray supArr && sub is JsonArray subArr)
        {
            if (subArr.TryGetRegoSetArray(out var subSet))
            {
                if (supArr.TryGetRegoSetArray(out var supSet))
                    return ObjectSubsetSet(supSet, subSet);

                return ObjectSubsetSet(supArr, subSet);
            }

            if (!super.IsRegoSet())
                return ObjectSubsetArray(supArr, subArr);
        }

        if (throwIfIncompatible)
            throw new InvalidOperationException("Both arguments object.subset must be of the same type or array and set");

        return false;
    }

    private static bool ObjectSubsetInner(JsonObject? super, JsonObject? sub)
    {
        if (super == null)
            return false;

        if (sub == null)
            return true;

        if (super.IsEquivalentTo(sub))
            return true;

        var result = false;

        foreach (var k in sub)
        {
            if (!super.TryGetPropertyValue(k.Key, out var supNode))
                return false;

            result = ObjectSubset(supNode, k.Value, false);

            if (!result)
                return false;
        }

        return result;
    }

    private static bool ObjectSubsetArray(JsonArray? super, JsonArray? sub)
    {
        if (super == null && sub == null)
            return true;

        if (super == null || sub == null)
            return false;

        if (sub.Count > super.Count)
            return false;

        var subCursor = 0;

        for (var i = 0; i < super.Count; i++)
        {
            for (var j = i; j < super.Count; j++)
            {
                if (j + sub.Count - subCursor > super.Count)
                    return false;

                if (!super[j].IsEquivalentTo(sub[subCursor]))
                {
                    subCursor = 0;
                    break;
                }

                subCursor++;

                if (subCursor >= sub.Count)
                    return true;
            }
        }

        return false;
    }

    private static bool ObjectSubsetSet(JsonArray super, JsonArray sub)
    {
        var hs = new HashSet<JsonNode?>(sub, JsonNodeEqualityComparer.Instance);
        return hs.IsSubsetOf(super);
    }

    private RegoSet<List<string>>? GraphReachablePaths(JsonNode? graph, JsonNode? initial)
    {
        if (graph is not JsonObject graphObj)
            return null;

        if (initial is not JsonArray initialArr)
            return null;

        var initialNodes = GetArrayEdges(initialArr);
        var result = new List<List<string>>();

        foreach (var node in initialNodes)
        {
            var edges = GetEdges(graphObj, node);

            if (edges == null)
                continue;

            if (edges.Count == 0)
                result.Add([node]);
            else
            {
                foreach (var e in edges)
                    Path(graphObj, e, [node], [], result);
            }
        }

        return new RegoSet<List<string>>(result);

        static List<string> GetArrayEdges(JsonArray ar) => ar.Select(p => p?.GetValue<string>()).Where(p => p != null).ToList()!;

        static List<string>? GetEdges(JsonObject g, string root)
        {
            if (g.TryGetPropertyValue(root, out var e) && e is JsonArray edges)
                return GetArrayEdges(edges);

            return null;
        }

        static void Path(
            JsonObject graph,
            string root,
            List<string> path,
            List<string> reached,
            List<List<string>> result)
        {
            var paths = new List<string>();

            var edges = GetEdges(graph, root);

            if (edges == null)
            {
                paths.AddRange(path);
                result.Add(paths);
            }
            else
            {
                path.Add(root);

                if (edges.Count == 0)
                {
                    paths.AddRange(path);
                    result.Add(paths);
                }
                else
                {
                    foreach (var edge in edges)
                    {
                        if (reached.Contains(edge))
                        {
                            paths.AddRange(path);
                            result.Add(paths);
                        }
                        else
                        {
                            reached.Add(root);
                            Path(graph, edge, [..path], reached, result);
                        }
                    }
                }
            }
        }
    }
}