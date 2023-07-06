﻿using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

using OpaDotNet.Wasm.Rego;

namespace OpaDotNet.Wasm;

public partial class DefaultOpaImportsAbi
{
    private static int[] IndexOfN(string haystack, string needle)
    {
        var result = new List<int>();
        var index = haystack.IndexOf(needle, StringComparison.Ordinal);

        while (index > -1)
        {
            result.Add(index);
            index = haystack.IndexOf(needle, index + 1, StringComparison.Ordinal);
        }

        return result.ToArray();
    }

    private static object? FormatString(JsonNode? node)
    {
        if (node == null)
            return null;

        if (node is JsonValue jv)
            return jv.ToJsonString().Trim('"');

        if (node is JsonArray ja)
        {
            if (ja.IsRegoSet())
            {
                if (!ja.TryGetRegoSetArray(out var setAr))
                    return null;

                var setStr = new List<string?>(setAr.Count);

                foreach (var el in setAr)
                    setStr.Add(el?.ToJsonString());

                return $"{{{string.Join(", ", setStr)}}}";
            }

            var arStr = new List<string?>(ja.Count);

            foreach (var el in ja)
                arStr.Add(el?.ToJsonString());

            return $"[{string.Join(", ", arStr)}]";
        }

        if (node is JsonObject jo)
            return jo.ToJsonString();

        return null;
    }

    private static IReadOnlyDictionary<char, Func<JsonNode?, (object?, char)>> _formats =
        new Dictionary<char, Func<JsonNode?, (object?, char)>>
        {
            { 's', p => (FormatString(p), 's') },
            { 'd', p => (p?.GetValue<int>(), 'd') },
            { 'b', p => (Convert.ToString(p!.GetValue<int>(), 2), 's') },
            { 'o', p => (Convert.ToString(p!.GetValue<int>(), 8), 's') },
            { 'x', p => (p!.GetValue<int>(), 'x') },
            { 'X', p => (p!.GetValue<int>(), 'X') },
            { 't', p => (p!.GetValue<bool>(), 's') },
            { 'e', p => (p!.GetValue<decimal>(), 'e') },
            { 'E', p => (p!.GetValue<decimal>(), 'E') },
            { 'f', p => (p!.GetValue<decimal>(), 'f') },
            { 'F', p => (p!.GetValue<decimal>(), 'F') },
            { 'g', p => (p!.GetValue<decimal>(), 'g') },
            { 'G', p => (p!.GetValue<decimal>(), 'G') },
        };

    private static string? Sprintf(string format, JsonNode? values)
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

            while (char.IsDigit(format[i]))
            {
                widthChars[++widthIndex] = format[i];
                i++;
            }

            var width = 0;

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

            var (value, fmt) = val(ja[valueIndex]);

            if (precision < 0 && fmt is 'f' or 'F')
            {
                // It seems rego takes precision 6 by default. 
                precision = 6;
            }

            var f = precision < 0 ? $"{{{0},{width}:{fmt}}}" : $"{{{0},{width}:{fmt}{precision}}}";
            result.AppendFormat(CultureInfo.InvariantCulture, f, value);

            valueIndex++;
            i++;
        }

        return result.ToString();
    }

    private static bool AnyPrefixMatch(IEnumerable<string> search, IEnumerable<string> baseStr)
    {
        return search.Any(p => baseStr.Any(p.StartsWith));
    }

    private static bool AnySuffixMatch(IEnumerable<string> search, IEnumerable<string> baseStr)
    {
        return search.Any(p => baseStr.Any(p.EndsWith));
    }
}