using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Web;

using JetBrains.Annotations;

using Json.More;

using Microsoft.Extensions.Primitives;

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

    [ExcludeFromCodeCoverage]
    protected virtual Random Random()
    {
        return _random;
    }

    private int RandIntN(string key, int n) => CacheGetOrAddValue($"rand.intn.{key}", () => Random().Next(n));

    private Guid NewGuid(string key) => CacheGetOrAddValue($"uuid.rfc4122.{key}", NewGuid);

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

    private object? YamlUnmarshal(string yamlString)
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
}