using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using OpaDotNet.Wasm.Tests;

using Yaml2JsonNode;

using YamlDotNet.Serialization;

namespace OpaDotNet.Wasm.Generators;

[Generator]
public class SdkV1TestCaseGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var filter = new SdkV1TestCaseFilter();

        var pipe = context.AdditionalTextsProvider
            .Where(static p => p.Path.EndsWith(".yaml"))
            .Select(
                (p, c) =>
                {
                    var fi = new FileInfo(p.Path);
                    var name = fi.Directory!.Name;

                    try
                    {
                        return SdkV1TestData.ParseFile(name, p.Path, filter);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to build tests cases for {p.Path}", ex);
                    }
                }
                )
            .WithComparer(SdkV1TestCaseContainerEqualityComparer.Instance);

        context.RegisterSourceOutput(
            pipe,
            static (context, p) =>
            {
                try
                {
                    if (p?.Cases.Count > 0)
                    {
                        var src = SdkV1TestWriter.WriteTestCases(p.Cases);
                        context.AddSource($"{p.FileName}.g.cs", SourceText.From(src, Encoding.UTF8));
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to generate source for {p?.FileName}: {ex.StackTrace}", ex);
                }
            }
            );
    }
}

[UsedImplicitly]
internal class TestCaseFilter
{
    public string Reason { get; set; } = null!;

    public HashSet<string> Regex { get; set; } = new();
}

internal class SdkV1TestCaseFilter
{
    private static readonly JsonSerializerOptions Opts = new(JsonSerializerOptions.Default)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly List<TestCaseFilter> _filters = LoadFilters();

    private static List<TestCaseFilter> LoadFilters()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var ignore = "OpaDotNet.Wasm.Generators.ignore.yaml";

        using var stream = assembly.GetManifestResourceStream(ignore);

        if (stream == null)
            return new List<TestCaseFilter>();

        using var reader = new StreamReader(stream);
        return YamlSerializer.Deserialize<List<TestCaseFilter>>(reader.ReadToEnd(), Opts) ?? new List<TestCaseFilter>();
    }

    public string? SkipReason(string testCaseName)
    {
        return _filters.FirstOrDefault(p => p.Regex.Any(pp => Regex.IsMatch(testCaseName, pp)))?.Reason;
    }
}

[UsedImplicitly]
internal class SdkV1TestCaseContainer
{
    private string? _fileName;

    public string FileName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_fileName))
                _fileName = Path.GetRandomFileName();

            return _fileName!;
        }
        set { _fileName = value; }
    }

    public HashSet<SdkV1TestCase> Cases { get; set; } = new();

    public string Hash { get; set; } = string.Empty;
}

internal class SdkV1TestCaseContainerEqualityComparer : IEqualityComparer<SdkV1TestCaseContainer?>
{
    public static SdkV1TestCaseContainerEqualityComparer Instance { get; } = new();

    public bool Equals(SdkV1TestCaseContainer? x, SdkV1TestCaseContainer? y) => string.Equals(x?.Hash, y?.Hash, StringComparison.Ordinal);

    public int GetHashCode(SdkV1TestCaseContainer? obj) => obj?.Hash.GetHashCode() ?? 0;
}

internal static class SdkV1TestData
{
    private static readonly JsonSerializerOptions Opts = new(JsonSerializerOptions.Default)
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static SdkV1TestCaseContainer? ParseFile(string name, string file, SdkV1TestCaseFilter filter)
    {
        var text = File.ReadAllText(file);
        var testCases = YamlSerializer.Deserialize<SdkV1TestCaseContainer>(text, Opts);

        if (testCases == null)
            return null;

        var testCaseName = Path.GetFileNameWithoutExtension(file);
        testCases.FileName = $"{name}-{testCaseName}";

        using var hashesStream = new MemoryStream();
        using var md5 = MD5.Create();
        var testCaseHashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(testCases.FileName));
        hashesStream.Write(testCaseHashBytes, 0, testCaseHashBytes.Length);

        foreach (var testCase in testCases.Cases)
        {
            testCase.Category = name;
            testCase.Name = testCaseName;

            var skipReason = filter.SkipReason(testCase.Note);

            if (!string.IsNullOrEmpty(skipReason))
                testCase.Skip = skipReason;
            else
            {
                var caseHash = md5.ComputeHash(Encoding.UTF8.GetBytes(testCase.Note));
                hashesStream.Write(caseHash, 0, caseHash.Length);
            }
        }

        hashesStream.Seek(0, SeekOrigin.Begin);
        testCases.Hash = Encoding.UTF8.GetString(md5.ComputeHash(hashesStream));

        return testCases;
    }
}