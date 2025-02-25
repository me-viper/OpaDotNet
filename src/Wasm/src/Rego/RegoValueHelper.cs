using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace OpaDotNet.Wasm.Rego;

internal static partial class RegoValueHelper
{
    [GeneratedRegex("{([A-z0-9\\\"][^:]*?({.+})*)}")]
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

    public static bool TryGetRegoSet<T>(
        this JsonNode? node,
        [MaybeNullWhen(false)] out RegoSet<T> set,
        JsonSerializerOptions? options = null)
    {
        set = null;

        if (node is not JsonArray ja)
            return false;

        return ja.TryGetRegoSet(out set, options);
    }

    public static bool TryGetRegoSetArray(
        this JsonArray ar,
        [MaybeNullWhen(false)] out JsonArray set)
    {
        ArgumentNullException.ThrowIfNull(ar);

        set = null;

        if (!ar.IsRegoSet())
            return false;

        try
        {
            set = ar[0]!["__rego_set"]!.AsArray();
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        return true;
    }

    public static bool TryGetRegoSet(
        this JsonArray ar,
        [MaybeNullWhen(false)] out RegoSet<JsonNode> set,
        JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(ar);

        set = null;

        if (!ar.IsRegoSet())
            return false;

        set = new RegoSet<JsonNode>(ar[0]!["__rego_set"]!.AsArray()!);

        return true;
    }

    public static bool TryGetRegoSet<T>(
        this JsonArray ar,
        [MaybeNullWhen(false)] out RegoSet<T> set,
        JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(ar);

        set = null;

        if (!ar.IsRegoSet())
            return false;

        set = ar[0]!.Deserialize<RegoSet<T>>(options);

        return set != null;
    }

    public static bool ContainsRegoSet(this JsonNode? node)
    {
        if (node == null)
            return false;

        if (node.IsRegoSet())
            return true;

        switch (node)
        {
            case JsonObject jo:
            {
                if (jo.Any(p => p.Value.ContainsRegoSet()))
                    return true;

                break;
            }
            case JsonArray ja:
            {
                if (ja.Any(p => p.ContainsRegoSet()))
                    return true;

                break;
            }
        }

        return false;
    }

    public static bool IsRegoSet(this JsonNode? node) => node is JsonArray ar && ar.IsRegoSet();

    public static bool IsRegoSet(this JsonArray? ar)
    {
        if (ar == null)
            return false;

        if (ar.Count != 1)
            return false;

        return ar[0] is JsonObject jo && jo.ContainsKey("__rego_set");
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

        return s.Replace("set()", "[{\"__rego_set\":[]}]");
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

                if (nativeSet.Length > 0)
                    s = s.Insert(arrayStartIndex, $"{{{nativeSet}}}");
                else
                    s = s.Insert(arrayStartIndex, "set()");
            }

            setStartIndex = s.LastIndexOf(setAnchor, StringComparison.Ordinal);
        }

        return s;
    }
}