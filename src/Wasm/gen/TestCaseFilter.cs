using System.Reflection;
using System.Text.RegularExpressions;

using Yaml2JsonNode;

namespace OpaDotNet.Wasm.Generators;

internal class TestCaseFilter
{
    private static readonly JsonSerializerOptions Opts = new(JsonSerializerOptions.Default)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly List<TestCaseFilterDefinition> _filters = LoadFilters();

    private static List<TestCaseFilterDefinition> LoadFilters()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var ignore = "OpaDotNet.Wasm.Generators.ignore.yaml";

        using var stream = assembly.GetManifestResourceStream(ignore);

        if (stream == null)
            return new List<TestCaseFilterDefinition>();

        using var reader = new StreamReader(stream);
        return YamlSerializer.Deserialize<List<TestCaseFilterDefinition>>(reader.ReadToEnd(), Opts) ?? new List<TestCaseFilterDefinition>();
    }

    public string? SkipReason(string testCaseName)
    {
        return _filters.FirstOrDefault(p => p.Regex.Any(pp => Regex.IsMatch(testCaseName, pp)))?.Reason;
    }
}