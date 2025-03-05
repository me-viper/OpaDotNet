using System.Collections;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Json.Pointer;

using OpaDotNet.Wasm.Tests.Common;

namespace OpaDotNet.Wasm.Tests;

public static class Ex
{
    public static JsonNode? Evaluate(this JsonPointer pointer, JsonNode root)
    {
        var current = root;
        var kind = root.GetValueKind();

        foreach (var segment in pointer)
        {
            switch (kind)
            {
                case JsonValueKind.Array:
                    var ar = current.AsArray();

                    if (segment.Length == 0)
                        return null;

                    if (segment is ['0'])
                    {
                        if (ar.Count == 0)
                            return null;

                        current = ar[0];
                        break;
                    }

                    if (segment[0] == '0')
                        return null;

                    if (segment is ['-'])
                        return ar.Count == 0 ? null : ar[^1];

                    if (!int.TryParse(segment, out var index))
                        return null;

                    if (index >= ar.Count)
                        return null;

                    if (index < 0)
                        return null;

                    current = ar[index];
                    break;

                case JsonValueKind.Object:
                    var found = false;
                    var obj = current.AsObject();

                    foreach (var p in obj)
                    {
                        if (segment != p.Key)
                            continue;

                        current = p.Value;
                        found = true;
                        break;
                    }

                    if (!found)
                        return null;

                    break;

                default:
                    return null;
            }

            if (current == null)
                return null;

            kind = current.GetValueKind();
        }

        return current;
    }
}

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
    [InlineData("""json.patch({}, [{"op":"add","path":"/","value":1}])""", """{"":1}""")]
    [InlineData("""json.patch({}, [{"op":"add","path":"","value":[]}])""", "[]")]
    [InlineData("""json.patch({"foo":{}}, [{"op":"add","path":"/foo/","value":1}])""", """{"foo":{"":1}}""")]
    public async Task JsonPatch(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""json.patch({"foo": {"a", "b", "c"}}, [{"op": "remove", "path": "foo/b"}])""", """{"foo":{"a","c"}}""")]
    [InlineData("""json.patch({"foo": {"a", "b", "c"}}, [{"op": "add", "path": "foo/d", "value": "d"}])""", """{"foo":{"a","b","c","d"}}""")]
    [InlineData(
        """json.patch({"foo": {"a", "b"}, "bar": {"c", "d"}}, [{"op": "move", "from": "foo/a", "path": "bar/a"}])""",
        """{"foo":{"b"},"bar":{"a","c","d"}}"""
        )]
    public async Task JsonPatchSet(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    // [Fact]
    // public void Xxx()
    // {
    //     var set = """
    //         {
    //           "foo" : [ {
    //             "__rego_set" : [ "c", "b", "a" ]
    //           } ]
    //         }
    //         """;
    //
    //     var node = JsonNode.Parse(set);
    //     var obj = node.ToJsonDocument();
    //
    //     var pp = JsonPointer.Parse("/foo/b");
    //     var ll = JsonPointer.Empty;
    //
    //     var currentElement = obj.RootElement;
    //
    //     for (var i = 0; i < pp.Count; i++)
    //     {
    //         ll = ll.Combine(pp[i]);
    //         var q = ll.Evaluate(currentElement);
    //
    //         if (q == null)
    //             throw new Exception();
    //
    //         if (q.Value.ValueKind != JsonValueKind.Array)
    //             continue;
    //
    //         if (q.Value.GetArrayLength() != 1)
    //             continue;
    //
    //         if (!q.Value[0].TryGetProperty("__rego_set", out var ss))
    //             continue;
    //
    //         if (ss.ValueKind != JsonValueKind.Array)
    //             continue;
    //
    //         if (i + 1 >= pp.Count)
    //             continue;
    //
    //         ll = ll.Combine(0, "__rego_set");
    //
    //         i++;
    //         var setMember = pp[i];
    //         var found = false;
    //
    //         for (var j = 0; j < ss.GetArrayLength(); j++)
    //         {
    //             if (ss[j].ValueEquals(setMember))
    //             {
    //                 ll = ll.Combine(j);
    //                 found = true;
    //                 break;
    //             }
    //         }
    //
    //         if (!found)
    //             throw new Exception();
    //     }
    //
    //     var op = PatchOperation.Remove(ll);
    //     var p = new JsonPatch(op);
    //     var result = p.Apply(node);
    //
    //     Assert.True(result.IsSuccess);
    //     Output.WriteLine(result.Result?.ToJsonString());
    // }

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