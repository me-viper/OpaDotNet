using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

using OpaDotNet.Wasm.Internal;
using OpaDotNet.Wasm.Rego;

namespace OpaDotNet.Wasm;

public partial class DefaultOpaImportsAbi
{
    private static int[] IndexOfN(string haystack, string needle)
    {
        if (haystack.Any(char.IsSurrogate))
            return IndexOfNSurogate(haystack, needle);

        var result = new List<int>();

        var index = haystack.IndexOf(needle, StringComparison.Ordinal);

        while (index > -1)
        {
            result.Add(index);
            index = haystack.IndexOf(needle, index + 1, StringComparison.Ordinal);
        }

        return result.ToArray();
    }

    private static int[] IndexOfNSurogate(string haystack, string needle)
    {
        var result = new List<int>();

        var n = UChars(needle).ToArray();
        var h = UChars(haystack).ToArray();

        for (var i = 0; i <= h.Length - n.Length; i++)
        {
            int k;

            for (k = 0; k < n.Length; k++)
            {
                if (!string.Equals(n[k], h[i + k], StringComparison.OrdinalIgnoreCase))
                    break;
            }

            if (k == n.Length)
                result.Add(i);
        }

        return result.ToArray();

        static IEnumerable<string> UChars(string s)
        {
            var e = StringInfo.GetTextElementEnumerator(s);

            while (e.MoveNext())
                yield return e.GetTextElement();
        }
    }

    private static object? FormatString(JsonNode? node, JsonSerializerOptions options)
    {
        if (node == null)
            return null;

        if (node is JsonValue jv)
            return jv.ToJsonString(options).Trim('"');

        if (node is JsonArray ja)
        {
            if (ja.IsRegoSet())
            {
                if (!ja.TryGetRegoSetArray(out var setAr))
                    return null;

                var setStr = new List<string?>(setAr.Count);

                foreach (var el in setAr)
                    setStr.Add(el?.ToJsonString(options));

                return $"{{{string.Join(", ", setStr)}}}";
            }

            var arStr = new List<string?>(ja.Count);

            foreach (var el in ja)
                arStr.Add(el?.ToJsonString(options));

            return $"[{string.Join(", ", arStr)}]";
        }

        if (node is JsonObject jo)
            return jo.ToJsonString(options);

        return null;
    }

    private static IReadOnlyDictionary<char, Func<JsonNode?, JsonSerializerOptions, (object?, char)>> _formats =
        new Dictionary<char, Func<JsonNode?, JsonSerializerOptions, (object?, char)>>
        {
            { 's', (p, o) => (FormatString(p, o), 's') },
            { 'd', (p, _) => (p?.GetValue<int>(), 'd') },
            { 'b', (p, _) => (p == null ? string.Empty : Convert.ToString(p.GetValue<int>(), 2), 's') },
            { 'o', (p, _) => (p == null ? string.Empty : Convert.ToString(p.GetValue<int>(), 8), 's') },
            { 'x', (p, _) => (p?.GetValue<int>(), 'x') },
            { 'X', (p, _) => (p?.GetValue<int>(), 'X') },
            { 't', (p, _) => (p?.GetValue<bool>(), 's') },
            { 'e', (p, _) => (p?.GetValue<decimal>(), 'e') },
            { 'E', (p, _) => (p?.GetValue<decimal>(), 'E') },
            { 'f', (p, _) => (p?.GetValue<decimal>(), 'f') },
            { 'F', (p, _) => (p?.GetValue<decimal>(), 'F') },
            { 'g', (p, _) => (p?.GetValue<decimal>(), 'g') },
            { 'G', (p, _) => (p?.GetValue<decimal>(), 'G') },
            { 'v', (p, o) => (FormatString(p, o), 's') },
        };

    private static string? Sprintf(string format, JsonNode? values, JsonSerializerOptions options)
    {
        if (values is not JsonArray ja)
            return null;

        var result = new StringBuilder();
        var i = 0;
        var valueIndex = 0;

        Span<char> widthChars = stackalloc char[10];
        Span<char> precisionChars = stackalloc char[10];

        while (i < format.Length)
        {
            if (format[i] != '%')
            {
                result.Append(format[i]);
                i++;
                continue;
            }

            i++;

            if (i >= format.Length)
                break;

            if (format[i] == '%')
            {
                result.Append('%');
                i++;
                continue;
            }

            widthChars.Clear();
            var widthIndex = -1;
            var padZeros = 1;

            while (char.IsDigit(format[i]))
            {
                if (format[i] == '0')
                    padZeros++;

                widthChars[++widthIndex] = format[i];
                i++;
            }

            var width = -1;

            if (widthIndex >= 0)
                width = int.Parse(widthChars[.. (widthIndex + 1)]);

            precisionChars.Clear();
            var precisionIndex = -1;

            if (format[i] == '.')
            {
                i++;

                while (char.IsDigit(format[i]))
                {
                    precisionChars[++precisionIndex] = format[i];
                    i++;
                }
            }

            var precision = -1;

            if (precisionIndex >= 0)
                precision = int.Parse(precisionChars[.. (precisionIndex + 1)]);

            if (!_formats.TryGetValue(format[i], out var val))
                throw new FormatException($"Unknown format {format[i]}");

            var (value, fmt) = val(ja[valueIndex], options);

            if (precision < 0 && width < 0 && fmt is 'f' or 'F' or 'e' or 'E' or 'g' or 'G')
            {
                // It seems rego takes precision 6 by default.
                precision = 6;
            }

            var formatStr = $"{fmt}";

            if (precision < 0)
                formatStr += padZeros;

            var f = precision < 0 ? $"{{{0},{width}:{formatStr}}}" : $"{{{0},{width}:{formatStr}{precision}}}";
            result.AppendFormat(CultureInfo.InvariantCulture, f, value);

            valueIndex++;
            i++;
        }

        return result.ToString();
    }

    private static bool? AnyPrefixMatch(JsonNode? search, JsonNode? baseStr)
    {
        if (!search.TryGetArray<string>(out var sa))
            return null;

        if (!baseStr.TryGetArray<string>(out var ba))
            return null;

        return AnyPrefixMatch(sa, ba);
    }

    private static bool AnyPrefixMatch(IEnumerable<string> search, IEnumerable<string> baseStr)
    {
        return search.Any(p => baseStr.Any(p.StartsWith));
    }

    private static bool? AnySuffixMatch(JsonNode? search, JsonNode? baseStr)
    {
        if (!search.TryGetArray<string>(out var sa))
            return null;

        if (!baseStr.TryGetArray<string>(out var ba))
            return null;

        return AnySuffixMatch(sa, ba);
    }

    private static bool AnySuffixMatch(IEnumerable<string> search, IEnumerable<string> baseStr)
    {
        return search.Any(p => baseStr.Any(p.EndsWith));
    }

    private static readonly IReadOnlySet<char> GlobChars = new HashSet<char>(['*', '?', '\\', '[', ']', '{', '}']);

    private static string GlobQuoteMeta(string pattern)
    {
        var sb = new StringBuilder();

        foreach (var ch in pattern)
        {
            if (GlobChars.Contains(ch))
                sb.Append('\\');

            sb.Append(ch);
        }

        return sb.ToString();
    }

    private static int StringCount(string search, string substring)
    {
        var count = 0;
        var i = 0;

        while (i < search.Length)
        {
            var index = search[i..].IndexOf(substring, StringComparison.Ordinal);

            if (index == -1)
                break;

            count++;
            i += index + substring.Length;
        }

        return count;
    }
}