using System.Text;
using System.Text.Json.Serialization;

using OpaDotNet.Wasm.Tests.Common;

namespace OpaDotNet.Wasm.Tests;

public class BasicsTests(ITestOutputHelper output) : OpaTestBase(output)
{
    private record PolicyResult
    {
        [JsonPropertyName("result")]
        public bool Result { get; [UsedImplicitly] set; }
    }

    private string BasePath { get; } = Path.Combine("TestData", "basics");

    public static IEnumerable<object?[]> BasicTestCases()
    {
        var versions = new[] { null, "1.0", "1.1", "1.2", "1.3" };

        const string data = "{ \"world\": \"world\" }";
        const string passInput = "{ \"message\": \"world\"}";
        const string failInput = "{ \"message\": \"world1\"}";

        foreach (var v in versions)
        {
            yield return
            [
                "simple.rego",
                "example/hello",
                data,
                passInput,
                v,
                true,
            ];

            yield return
            [
                "simple.rego",
                "example/hello",
                data,
                failInput,
                v,
                false,
            ];

            yield return
            [
                "nodata.rego",
                "example/hello",
                null,
                passInput,
                v,
                true,
            ];

            yield return
            [
                "nodata.rego",
                "example/hello",
                null,
                failInput,
                v,
                false,
            ];

            yield return
            [
                "entrypoints.rego",
                null,
                data,
                passInput,
                v,
                true,
            ];

            yield return
            [
                "entrypoints.rego",
                null,
                data,
                failInput,
                v,
                false,
            ];
        }
    }

    [Theory]
    [MemberData(nameof(BasicTestCases))]
    public async Task MultipleRunsFromSource(
        string source,
        string? entrypoint,
        string? data,
        string input,
        string? abiVersion,
        bool expectedResult)
    {
        var ver = string.IsNullOrWhiteSpace(abiVersion) ? null : Version.Parse(abiVersion);

        var entrypoints = string.IsNullOrWhiteSpace(entrypoint) ? null : new[] { entrypoint };
        await using var policy = await CompileFile(Path.Combine(BasePath, source), entrypoints);

        using var engine = OpaBundleEvaluatorFactory.Create(
            policy,
            options: new() { MaxAbiVersion = ver }
            );

        engine.SetDataFromRawJson(data);

        var result1Str = engine.EvaluateRaw(input, entrypoint);
        Assert.NotEmpty(result1Str);

        Output.WriteLine(result1Str);

        var result1 = JsonSerializer.Deserialize<PolicyResult[]>(result1Str);

        Assert.NotNull(result1);
        Assert.Collection(result1, p => Assert.Equal(expectedResult, p.Result));

        var result2Str = engine.EvaluateRaw(input);
        Assert.NotEmpty(result2Str);

        Output.WriteLine(result2Str);

        var result2 = JsonSerializer.Deserialize<PolicyResult[]>(result2Str);

        Assert.NotNull(result2);
        Assert.Collection(result2, p => Assert.Equal(expectedResult, p.Result));
    }

    [Theory]
    [InlineData("{ \"world\": \"world\" }", "{ \"message\": \"world\"}", true)]
    [InlineData("{ \"world\": \"world\" }", "{ \"message\": \"world1\"}", false)]
    public void StringData(string? data, string input, bool expectedResult)
    {
        using var policy = File.OpenRead(Path.Combine(BasePath, "simple-1.2.wasm"));
        using var engine = OpaWasmEvaluatorFactory.Create(policy);

        engine.SetDataFromRawJson(data);

        var resultStr = engine.EvaluateRaw(input);
        var result = JsonSerializer.Deserialize<PolicyResult[]>(resultStr);

        Assert.NotNull(result);
        Assert.Collection(result, p => Assert.Equal(expectedResult, p.Result));

        Output.WriteLine(resultStr);

        Assert.NotEmpty(resultStr);
    }

    [Theory]
    [InlineData("{ \"world\": \"world\" }", "{ \"message\": \"world\"}", true)]
    [InlineData("{ \"world\": \"world\" }", "{ \"message\": \"world1\"}", false)]
    public Task StreamData(string data, string input, bool expectedResult)
    {
        using var dataStream = new MemoryStream();
        var buffer = Encoding.UTF8.GetBytes(data);
        dataStream.Write(buffer);
        dataStream.Seek(0, SeekOrigin.Begin);

        using var policy = File.OpenRead(Path.Combine(BasePath, "simple-1.2.wasm"));
        using var engine = OpaWasmEvaluatorFactory.Create(policy);

        engine.SetDataFromStream(dataStream);

        var resultStr = engine.EvaluateRaw(input);
        var result = JsonSerializer.Deserialize<PolicyResult[]>(resultStr);

        Assert.NotNull(result);
        Assert.Collection(result, p => Assert.Equal(expectedResult, p.Result));

        Output.WriteLine(resultStr);

        Assert.NotEmpty(resultStr);
        return Task.CompletedTask;
    }

    private record Data([UsedImplicitly] string World);

    [Theory]
    [InlineData("{ \"message\": \"world\"}", true)]
    [InlineData("{ \"message\": \"world1\"}", false)]
    public void TypedData(string input, bool expectedResult)
    {
        var opts = WasmPolicyEngineOptions.DefaultWithJsonOptions(
            p =>
            {
                p.PropertyNameCaseInsensitive = true;
                p.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            }
            );

        using var policy = File.OpenRead(Path.Combine(BasePath, "simple-1.2.wasm"));

        using var engine = OpaWasmEvaluatorFactory.Create(
            policy,
            opts
            );

        engine.SetData(new Data("world"));

        var resultStr = engine.EvaluateRaw(input);
        var result = JsonSerializer.Deserialize<PolicyResult[]>(resultStr, opts.SerializationOptions);

        Assert.NotNull(result);
        Assert.Collection(result, p => Assert.Equal(expectedResult, p.Result));

        Output.WriteLine(resultStr);

        Assert.NotEmpty(resultStr);
    }

    private record CompositeResult
    {
        public string? X { [UsedImplicitly] get; set; }
        public int Y { [UsedImplicitly] get; set; }
        public bool Z { [UsedImplicitly] get; set; }
    }

    [Fact]
    public async Task CompositeTests()
    {
        var opts = new WasmPolicyEngineOptions
        {
            SerializationOptions = new() { PropertyNameCaseInsensitive = true }
        };

        await using var policy = await CompileFile(Path.Combine(BasePath, "composite.rego"), ["example"]);

        using var engine = OpaBundleEvaluatorFactory.Create(policy, options: opts);
        var result = engine.Evaluate<object?, CompositeResult>(null, "example");

        var expected = new CompositeResult { X = "hi", Y = 1, Z = true };

        Assert.NotNull(result);
        Assert.Equal(expected, result.Result);
    }

    [Fact]
    public async Task EmptyOutput()
    {
        await using var policy = await CompileFile(Path.Combine(BasePath, "empty_composite.rego"), ["example"]);
        using var factory = new OpaBundleEvaluatorFactory(policy);

        using var engine = factory.Create();

        var result = engine.Evaluate<object?, CompositeResult>(null, "example");

        var expected = new CompositeResult { X = null, Y = 0, Z = false };

        Assert.NotNull(result);
        Assert.Equal(expected, result.Result);
    }

    private record Input
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = default!;
    }

    [Theory]
    [InlineData("{ \"message\": \"world\"}", true)]
    [InlineData("{ \"message\": \"world1\"}", false)]
    public void Eval(string input, bool expected)
    {
        var inp = JsonSerializer.Deserialize<Input>(input);

        Assert.NotNull(inp);

        using var engine = OpaWasmEvaluatorFactory.Create(
            File.OpenRead(Path.Combine(BasePath, "simple-1.2.wasm"))
            );

        engine.SetDataFromRawJson("{ \"world\": \"world\" }");

        var result1 = engine.EvaluatePredicate(inp);
        Assert.Equal(expected, result1.Result);

        var result2 = engine.Evaluate<Input, JsonElement>(inp);
        Assert.Equal(expected, result2.Result.GetBoolean());

        var result3 = engine.EvaluateRaw(input);
        var expectedStr = expected ? "true" : "false";
        var expectedResult = $"[{{\"result\":{expectedStr}}}]";
        Assert.Equal(expectedResult, result3);
    }

    [Theory]
    [InlineData("{ \"message\": \"world\"}", true)]
    [InlineData("{ \"message\": \"world1\"}", false)]
    public void UpdateData(string input, bool expected)
    {
        var inp = JsonSerializer.Deserialize<Input>(input);

        Assert.NotNull(inp);

        using var engine = OpaWasmEvaluatorFactory.Create(
            File.OpenRead(Path.Combine(BasePath, "simple-1.2.wasm"))
            );

        engine.SetDataFromRawJson("{ \"world\": \"world\" }");

        var result1 = engine.EvaluatePredicate(inp);
        Assert.Equal(expected, result1.Result);

        engine.SetDataFromRawJson("{ \"world\": \"world1\" }");

        var result2 = engine.EvaluatePredicate(inp);
        Assert.Equal(expected, !result2.Result);
    }

    [Fact]
    public async Task EmptyPredicate()
    {
        var policy = await CompileFile(Path.Combine(BasePath, "simple.rego"), ["example/empty"]);
        using var factory = new OpaBundleEvaluatorFactory(policy);

        using var engine = factory.Create();

        var result = engine.EvaluatePredicate<object?>(null, "example/empty");

        Assert.NotNull(result);
        Assert.False(result.Result);
    }

    [Fact]
    public async Task PrimitiveInput()
    {
        var src = """
            package example
            import rego.v1
            p if input == 1.0
            """;

        var policy = await CompileSource(src, ["example/p"]);
        using var factory = new OpaBundleEvaluatorFactory(policy);
        using var engine = factory.Create();

        var result = engine.EvaluatePredicate(1, "example/p");
        Assert.True(result.Result);
    }

    [Fact]
    public async Task PrimitiveInput2()
    {
        var src = """
            package example
            import rego.v1
            p if input == 1.0
            """;

        var policy = await CompileSource(src, ["example/p"]);
        using var factory = new OpaBundleEvaluatorFactory(policy);
        using var engine = factory.Create();

        var result = engine.EvaluatePredicate(1.0, "example/p");
        Assert.True(result.Result);
    }

    [Fact]
    public async Task PrimitiveInput3()
    {
        var src = """
            package example
            import rego.v1
            p if input == 1.0
            """;

        var policy = await CompileSource(src, ["example/p"]);
        using var factory = new OpaBundleEvaluatorFactory(policy);
        using var engine = factory.Create();

        var jsonResult = engine.EvaluateRaw("1.0", "example/p");
        var result = JsonSerializer.Deserialize<PolicyEvaluationResult<bool>[]>(jsonResult);

        Assert.NotNull(result);
        Assert.Single(result, p => p.Result);
    }

    [Fact]
    public async Task RegoSetInput()
    {
        var src = """
            package example
            import rego.v1
            p := input
            """;

        var policy = await CompileSource(src, ["example/p"]);
        using var factory = new OpaBundleEvaluatorFactory(policy);
        using var engine = factory.Create();

        var jsonResult = engine.EvaluateRaw("{1,2,3}", "example/p");
        var result = JsonSerializer.Deserialize<PolicyEvaluationResult<int[]>[]>(jsonResult);

        Assert.NotNull(result);
        Assert.Collection(result[0].Result, p => Assert.Equal(1, p), p => Assert.Equal(2, p), p => Assert.Equal(3, p));
    }

    [Fact]
    public void Reset()
    {
        var data = "{\"world\":\"world\"}";

        using var engine = (OpaWasmEvaluator)OpaWasmEvaluatorFactory.Create(
            File.OpenRead(Path.Combine(BasePath, "simple-1.3.wasm"))
            );

        engine.SetDataFromRawJson(data);

        var result1 = engine.DumpData();
        Assert.Equal(data, result1);

        engine.Reset();

        var result2 = engine.DumpData();
        Assert.Empty(result2);
    }
}