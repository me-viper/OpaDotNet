using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace OpaDotNet.Wasm.Rego;

internal static class RegoValueHelper
{
    public static bool TryParseTuple<T>(JsonArray ar, [MaybeNullWhen(false)] out Tuple<T> tuple)
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

    public static string SetFromJson(string s)
    {
        const string setAnchor = "\"__rego_set\"";

        var setStartIndex = s.LastIndexOf(setAnchor, StringComparison.Ordinal);

        while (setStartIndex > 0)
        {
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

                s = s.Remove(setStartIndex, nativeSet.Length + setAnchor.Length + 3);
                s = s.Insert(setStartIndex, nativeSet);
            }

            setStartIndex = s.LastIndexOf(setAnchor, StringComparison.Ordinal);
        }

        return s;
    }
}