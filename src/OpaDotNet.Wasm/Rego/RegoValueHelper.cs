using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace OpaDotNet.Wasm.Rego;

internal static partial class RegoValueHelper
{
    [GeneratedRegex("{([^:]*?({.+})*)}")]
    private static partial Regex RegoSetRegex();

    public static bool TryGetTuple<T>(this JsonArray ar, [MaybeNullWhen(false)] out Tuple<T> tuple)
    {
        ArgumentNullException.ThrowIfNull(ar);

        tuple = null;

        if (ar.Count < 1)
            return false;

        if (ar[0] is not JsonValue)
            return false;

        var val = ar[0]!.GetValue<T>();
        tuple = Tuple.Create(val);

        return true;
    }

    public static bool TryGetRegoSet<T>(this JsonNode? node, [MaybeNullWhen(false)] out RegoSet<T> set)
    {
        set = null;

        if (node is not JsonArray ja)
            return false;

        return ja.TryGetRegoSet(out set);
    }

    public static bool TryGetRegoSet<T>(this JsonArray ar, [MaybeNullWhen(false)] out RegoSet<T> set)
    {
        ArgumentNullException.ThrowIfNull(ar);

        set = null;

        if (!ar.IsRegoSet())
            return false;

        set = ar[0]!.Deserialize<RegoSet<T>>(WasmPolicyEngineOptions.JsonSerializationOptions);

        return set != null;
    }

    public static bool IsRegoSet(this JsonNode? node)
    {
        if (node == null)
            return false;

        ArgumentNullException.ThrowIfNull(node);

        if (node is not JsonArray ar)
            return false;

        return ar.IsRegoSet();
    }

    public static bool IsRegoSet(this JsonArray ar)
    {
        ArgumentNullException.ThrowIfNull(ar);

        if (ar.Count != 1)
            return false;

        if (ar[0] is not JsonObject jo)
            return false;

        return jo.ContainsKey("__rego_set");
    }

    public static string JsonFromRegoValue(string s)
    {
        var regex = RegoSetRegex();

        var maxIterations = s.Length;
        var currentIteration = 0;

        while (regex.IsMatch(s))
        {
            if (currentIteration++ > maxIterations)
                break;

            s = regex.Replace(s, "[{\"__rego_set\":[$1]}]");
        }

        return s;
    }

    public static string JsonToRegoValue(string s)
    {
        const string setAnchor = "\"__rego_set\"";

        var setStartIndex = s.LastIndexOf(setAnchor, StringComparison.Ordinal);
        var maxIterations = s.Length;
        var iterations = 0;

        while (setStartIndex > 0)
        {
            if (iterations++ > maxIterations)
                break;

            var i = setStartIndex;
            var depth = -1;
            var start = -1;
            var end = -1;

            while (i < s.Length)
            {
                if (s[i] == '[')
                {
                    if (depth == -1)
                        start = i;

                    depth++;
                }

                if (s[i] == ']')
                {
                    if (depth == 0)
                    {
                        end = i;
                        break;
                    }

                    depth--;
                }

                i++;
            }

            if (start > -1 && end > -1)
            {
                var nativeSet = s[(start + 1)..end];

                var arrayStartIndex = -1;
                var asi = setStartIndex;

                while (asi >= 0)
                {
                    if (s[asi] == '[')
                    {
                        arrayStartIndex = asi;
                        break;
                    }

                    asi--;
                }

                if (arrayStartIndex < 0)
                    return s;

                var arrayEndIndex = -1;
                var aei = end + 1;

                while (aei < s.Length)
                {
                    if (s[aei] == ']')
                    {
                        arrayEndIndex = aei;
                        break;
                    }

                    aei++;
                }

                if (arrayEndIndex < 0)
                    return s;

                s = s.Remove(arrayStartIndex, arrayEndIndex - arrayStartIndex + 1);
                s = s.Insert(arrayStartIndex, $"{{{nativeSet}}}");
            }

            setStartIndex = s.LastIndexOf(setAnchor, StringComparison.Ordinal);
        }

        return s;
    }
}