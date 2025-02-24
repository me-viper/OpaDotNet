using System.Security.Cryptography;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using Yaml2JsonNode;

namespace OpaDotNet.Wasm.Generators;

internal static class TestCaseParser
{
    private static readonly JsonSerializerOptions Opts = new(JsonSerializerOptions.Default)
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static SdkV1TestCaseContainer? ParseFile(AdditionalText file, TestCaseFilter filter)
    {
        var fi = new FileInfo(file.Path);
        var category = fi.Directory!.Name;
        var testCaseName = Path.GetFileNameWithoutExtension(fi.Name);
        var source = file.GetText();

        if (source == null)
            return null;

        var sb = new StringBuilder();

        using (var sw = new StringWriter(sb))
            source.Write(sw);

        var text = sb.ToString();

        if (string.IsNullOrWhiteSpace(text))
            return null;

        var testCases = YamlSerializer.Deserialize<SdkV1TestCaseContainer>(text, Opts);

        if (testCases == null)
            return null;

        testCases.Name = $"{category}-{testCaseName}";
        testCases.FileName = fi.FullName;

        using var hashesStream = new MemoryStream();
        using var md5 = MD5.Create();
        var testCaseHashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(testCases.Name));
        hashesStream.Write(testCaseHashBytes, 0, testCaseHashBytes.Length);

        foreach (var testCase in testCases.Cases)
        {
            testCase.Category = category;
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