using System.Text;
using System.Text.Json.Nodes;

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

    private static string? Sprintf(string format, JsonNode? values)
    {
        if (values is not JsonArray ja)
            return null;

        var result = new StringBuilder();

        foreach (var val in ja)
        {
            result.Append(val);
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