using System.Text.RegularExpressions;

using OpaDotNet.Wasm.Rego;

namespace OpaDotNet.Benchmarks;

[Config(typeof(Config))]
public partial class OpaJsonReaderBench
{
    [GeneratedRegex("{([^:]*?({.+})*)}")]
    private static partial Regex RegoSetRegex();

    private const string RegoToJson = """{1,{"a":{"y","z"}}}""";

    [Benchmark]
    [BenchmarkCategory("Rego => Json")]
    public string JsonFromRegoValueRegex()
    {
        return JsonFromRegoValue(RegoToJson);
    }

    [Benchmark]
    [BenchmarkCategory("Rego => Json")]
    public object JsonFromRegoValueOpaJsonReader()
    {
        return RegoValueHelper.JsonFromRegoValue(RegoToJson);
    }

    private static string JsonFromRegoValue(string s)
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

        s = s.Replace("[{\"__rego_set\":[]}]", "{}");
        return s.Replace("set()", "[{\"__rego_set\":[]}]");
    }

    private static string JsonToRegoValue(string s)
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