using System.Buffers.Text;
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

using FormatException = System.FormatException;

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
#if NET9_0_OR_GREATER
        return Base64Url.DecodeFromChars(x);
#else
        var s = x.PadRight(x.Length + (4 - x.Length % 4) % 4, '=')
            .Replace('-', '+')
            .Replace('_', '/');

        return Convert.FromBase64String(s);
#endif
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

    private static Dictionary<string, List<string>> UrlQueryDecodeObject(string x)
    {
        var result = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(x))
            return result;

        var queryIndex = x.IndexOf('?');
        queryIndex = queryIndex == -1 ? 0 : queryIndex;

        var q = x[queryIndex..];

        var queryParts = new StringTokenizer(q, ['&']);

        foreach (var part in queryParts)
        {
            var eqIndex = part.IndexOf('=');

            string key;
            string value;

            if (eqIndex == -1)
            {
                key = part.ToString();
                value = string.Empty;
            }
            else
            {
                key = part.Substring(0, eqIndex);
                value = HttpUtility.UrlDecode(part.Substring(eqIndex + 1));
            }

            if (!result.ContainsKey(key))
                result.Add(key, []);

            result[key].Add(value);
        }

        return result;
    }

    private static string UrlQueryEncode(string x)
    {
        Span<char> s = HttpUtility.UrlEncode(x).ToCharArray();

        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] != '%')
                continue;

            if (i + 3 >= s.Length)
                continue;

            i++;

            for (var j = 1; j < 3; j++)
            {
                s[i] = char.ToUpperInvariant(s[i]);
                i++;
            }

            i--;
        }

        return s.ToString();
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
                var values = ja.Select(p => $"{k}={UrlQueryEncode(p!.GetValue<string>())}");
                result.Add(string.Join('&', values));
                continue;
            }

            result.Add($"{k}={UrlQueryEncode(v!.GetValue<string>())}");
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
            throw new FormatException($"string \"{a}\" is not a valid SemVer");

        if (!SemVersion.TryParse(b, SemVersionStyles.Strict, out var vb))
            throw new FormatException($"string \"{b}\" is not a valid SemVer");

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
        if (string.IsNullOrWhiteSpace(yamlString))
            return null;

        var deserializer = new DeserializerBuilder().WithAttemptingUnquotedStringTypeDeserialization().Build();
        var result = deserializer.Deserialize(new StringReader(yamlString));
        return result?.ToJsonDocument();
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
            throw new ArgumentException("No amount provided", nameof(x));

        const decimal milli = 0.001m;

        var num = double.NaN;
        var unit = string.Empty;

        x = x.Replace("\"", "");

        if (x[0] == ' ')
            throw new FormatException("Spaces not allowed in resource strings");

        for (var i = x.Length - 1; i >= 0; i--)
        {
            if (char.IsLetter(x[i]))
                continue;

            if (x[i] == ' ')
                throw new FormatException("Spaces not allowed in resource strings");

            if (!double.TryParse(x[.. (i + 1)], CultureInfo.InvariantCulture, out num))
                throw new FormatException("Could not parse amount to a number");

            if (i + 1 < x.Length)
            {
                unit = x[(i + 1) ..];
                unit = $"{unit[0]}{unit[1..].ToLowerInvariant()}";
            }

            break;
        }

        if (double.IsNaN(num))
            throw new FormatException("No amount provided");

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
            _ => throw new FormatException("No amount provided"),
        };

        return n.Value * (decimal)num;
    }

    private static ulong? UnitsParseBytes(string x)
    {
        if (string.IsNullOrWhiteSpace(x))
            throw new ArgumentException("No byte amount provided", nameof(x));

        var num = double.NaN;
        var unit = string.Empty;

        x = x.Replace("\"", "");

        if (x[0] == ' ')
            throw new FormatException("Spaces not allowed in resource strings");

        for (var i = x.Length - 1; i >= 0; i--)
        {
            if (char.IsLetter(x[i]))
                continue;

            if (x[i] == ' ')
                throw new FormatException("Spaces not allowed in resource strings");

            if (!double.TryParse(x[.. (i + 1)], CultureInfo.InvariantCulture, out num))
                throw new FormatException("Could not parse byte amount to a number");

            if (i + 1 < x.Length)
                unit = x[(i + 1) ..].ToLowerInvariant();

            break;
        }

        if (double.IsNaN(num))
            throw new FormatException("No byte amount provided");

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
            _ => throw new FormatException("No byte amount provided"),
        };

        var result = n.Value * (decimal)num;

        return decimal.ToUInt64(result);
    }

    private static int[]? NumbersRangeStep(int a, int b, int step)
    {
        if (step < 1)
            throw new ArgumentOutOfRangeException(nameof(step), "Step must be a positive number above zero");

        if (a == b)
            return [a];

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

    private static RegoSet<List<string>> GraphReachablePaths(JsonNode? graph, JsonNode? initial)
    {
        if (graph is not JsonObject graphObj)
            throw new ArgumentException($"{typeof(JsonObject)} expected", nameof(graph));

        if (initial is not JsonArray initialArr)
            throw new ArgumentException($"{typeof(JsonArray)} expected", nameof(initial));

        if (graphObj.Count == 0)
            return new RegoSet<List<string>>([]);

        var initialNodes = GetArrayEdges(initialArr);
        var result = new List<List<string>>();

        foreach (var node in initialNodes)
        {
            var edges = GetEdges(graphObj, node);

            if (edges == null || edges.Count == 0)
                result.Add([node]);
            else
            {
                foreach (var e in edges)
                    Path(graphObj, e, [node], [node], result);
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

    private static T GetValue<T>(JsonArray ar, int index)
    {
        if (ar[index] is JsonValue jv)
            return jv.GetValue<T>();

        throw new ArgumentException("Invalid parameter");
    }

    private static Tuple<T1, T2, T3> ParseArgs<T1, T2, T3>(JsonArray ar, byte requiredArgs = 3)
    {
        // In general case this is wrong. But JsonValue.GetValue<T>() ensures we've got valid type.
        T1 t1 = default!;
        T2 t2 = default!;
        T3 t3 = default!;

        if (requiredArgs < 1)
            throw new ArgumentOutOfRangeException(nameof(requiredArgs));

        if (ar.Count < requiredArgs)
            throw new ArgumentException("Invalid parameter");

        t1 = GetValue<T1>(ar, 0);

        if (ar.Count <= 1)
            return Tuple.Create(t1, t2, t3);

        t2 = GetValue<T2>(ar, 1);

        if (ar.Count <= 2)
            return Tuple.Create(t1, t2, t3);

        t3 = GetValue<T3>(ar, 2);

        return Tuple.Create(t1, t2, t3);
    }

    private static Tuple<T1, T2> ParseArgs<T1, T2>(JsonArray ar)
    {
        T1 t1;
        T2 t2;

        if (ar.Count != 2)
            throw new ArgumentException("Invalid parameter");

        if (ar[0] is JsonValue jv0)
            t1 = jv0.GetValue<T1>();
        else
            throw new ArgumentException("Invalid parameter");

        if (ar[1] is JsonValue jv1)
            t2 = jv1.GetValue<T2>();
        else
            throw new ArgumentException("Invalid parameter");

        return Tuple.Create(t1, t2);
    }

}