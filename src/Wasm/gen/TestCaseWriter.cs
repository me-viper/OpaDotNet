using System.Text;
using System.Text.Json.Nodes;

using Microsoft.CodeAnalysis.CSharp;

using OpaDotNet.Wasm.Tests;

namespace OpaDotNet.Wasm.Generators;

internal static class TestCaseWriter
{
    public static string WriteTestCases(IEnumerable<SdkV1TestCase> cases)
    {
        var sb = new StringBuilder();
        var i = 0;

        foreach (var tc in cases)
        {
            if (i > 0)
                sb.AppendLine();

            sb.AppendLine(WriteTestCase(tc, ++i));
            sb.AppendLine();
        }

        return $@"namespace OpaDotNet.Wasm.Tests.SdkV1;

public partial class SdkV1Tests
{{
    {sb}
}}";
    }

    private static string WriteJsonNodeCode(JsonNode? node)
    {
        return node == null
            ? "null"
            : $"System.Text.Json.Nodes.JsonNode.Parse(\n\"\"\"\n{node.ToJsonString()}\n\"\"\"\n)";
    }

    private static string WriteJsonArrayCode(JsonArray? node)
    {
        return node == null
            ? "null"
            : $"System.Text.Json.Nodes.JsonNode.Parse(\n\"\"\"\n{node.ToJsonString()}\n\"\"\"\n).AsArray()";
    }

    private static string WriteJsonArrayCode(JsonValue? node)
    {
        return node == null
            ? "null"
            : $"System.Text.Json.Nodes.JsonNode.Parse(\n\"\"\"\n{node.ToJsonString()}\n\"\"\"\n).AsValue()";
    }

    private static string FormatFunctionName(string name)
    {
        var r = new StringBuilder(name.Length);
        var haveFirstChar = false;

        for (var i = 0; i < name.Length; i++)
        {
            if (!haveFirstChar && !char.IsLetter(name[i]))
                continue;

            var ch = name[i];

            if (!char.IsLetterOrDigit(ch))
                ch = '_';

            r.Append(ch);
            haveFirstChar = true;
        }

        return r.ToString();
    }

    private static string WriteTestCase(SdkV1TestCase testCase, int caseN)
    {
        var testName = $"{testCase.Category}__{testCase.Name}";
        var name = $"{FormatFunctionName(testName)}_{caseN}";

        var note = SymbolDisplay.FormatLiteral(testCase.Note, true);

        if (!string.IsNullOrWhiteSpace(testCase.Skip))
        {
            return $@"
        [Fact(DisplayName = {note}, Skip = """"""{testCase.Skip}"""""")]
        [System.Runtime.CompilerServices.CompilerGenerated]
        public Task {name}()
        {{
            return Task.CompletedTask;
        }}";
        }

        var modules = string.Join(",", testCase.Modules.Select(FormatModule));

        return $@"
        [Fact(DisplayName = {note})]
        [System.Runtime.CompilerServices.CompilerGenerated]
        public async Task {name}()
        {{
            var testCase = new SdkV1TestCase();

            testCase.Name = ""{name}"";
            testCase.Note = {note};
            testCase.Query = {SymbolDisplay.FormatLiteral(testCase.Query, true)};
            testCase.Modules = [
                {modules}
            ];
            testCase.WantResult = {WriteJsonArrayCode(testCase.WantResult)};
            testCase.WantErrorCode = {SymbolDisplay.FormatLiteral(testCase.WantErrorCode ?? string.Empty, true)};
            testCase.WantError = {SymbolDisplay.FormatLiteral(testCase.WantError ?? string.Empty, true)};
            testCase.StrictError = {testCase.StrictError.ToString().ToLowerInvariant()};
            testCase.Data = {WriteJsonNodeCode(testCase.Data)};
            testCase.Input = {WriteJsonNodeCode(testCase.Input)};
            testCase.InputTerm = {SymbolDisplay.FormatLiteral(testCase.InputTerm ?? string.Empty, true)};
            testCase.SortBindings = {testCase.SortBindings.ToString().ToLowerInvariant()};

            ApplyTestCaseShims(testCase);

            await RunTestCase(testCase);
        }}";
    }

    private static string FormatModule(string module)
    {
        return $"\"\"\"\n{module}\n\"\"\"";
    }
}