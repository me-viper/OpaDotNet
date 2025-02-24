using System.Collections;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using OpaDotNet.Wasm.Tests.Common;

using JsonException = System.Text.Json.JsonException;

namespace OpaDotNet.Wasm.Tests;

public partial class SdkJsonPatchTests(ITestOutputHelper output) : SdkTestBase(output)
{
    public class JsonPatchTheory
    {
        public string Comment { get; set; } = null!;

        [JsonPropertyName("opa_disabled")]
        public bool Skip { get; set; }

        public bool Disabled { get; set; }

        public JsonNode Doc { get; set; } = null!;

        public JsonNode Patch { get; set; } = null!;

        public JsonNode? Expected { get; set; }

        public string? Error { get; set; }

        public override string ToString() => Comment;
    }

    private static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static JsonDocumentOptions DocumentOptions { get; } = new()
    {
        AllowTrailingCommas = true,
    };

    internal class TestData(string source) : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            var tests = JsonNode.Parse(source, null, DocumentOptions)!.AsArray();

            foreach (var el in tests)
            {
                var jpt = el.Deserialize<JsonPatchTheory>(SerializerOptions);

                if (jpt == null)
                    throw new InvalidOperationException("Failed to deserialize test case");

                if (jpt.Skip || jpt.Disabled)
                    continue;

                if (string.IsNullOrWhiteSpace(jpt.Comment))
                    jpt.Comment = jpt.Error ?? el!.ToJsonString();

                yield return [jpt];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal class JsonSpecTestData() : TestData(JsonSpecTests);

    internal class JsonTestData() : TestData(JsonTests);

    [Theory]
    [InlineData("""json.patch({"a": {"foo": 1}}, [{"op": "add", "path": "/a/bar", "value": 2}])""", """{"a": {"foo": 1, "bar": 2}}""")]
    public async Task JsonPatch(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    private static WasmPolicyEngineOptions WasmOptions { get; } = new()
    {
        StrictBuiltinErrors = true,
        SerializationOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        },
        SignatureValidation = new() { Validation = SignatureValidationType.Skip },
    };

    [Theory]
    [ClassData(typeof(JsonSpecTestData))]
    public Task Spec(JsonPatchTheory theory) => Run(theory);

    [Theory]
    [ClassData(typeof(JsonTestData))]
    public Task Json(JsonPatchTheory theory) => Run(theory);

    private async Task Run(JsonPatchTheory theory)
    {
        var func = $$"""
            json.patch({{theory.Doc.ToJsonString()}}, {{theory.Patch.ToJsonString()}})
            """;

        if (theory.Expected == null)
            await Assert.ThrowsAsync<OpaEvaluationException>(() => RunTestCase(func, "{}", options: WasmOptions));
        else
        {
            var result = await RunTestCase(func, theory.Expected.ToJsonString(), options: WasmOptions);
            Assert.True(result.Assert);
        }
    }
}