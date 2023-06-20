using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class BasicsTests
{
    private record PolicyResult
    {
        [JsonPropertyName("result")]
        public bool Result { get; set; }
    }

    private readonly ITestOutputHelper _output;

    private readonly ILoggerFactory _loggerFactory;

    private string BasePath { get; } = Path.Combine("TestData", "Opa", "basics");

    private readonly OptionsWrapper<RegoCliCompilerOptions> _options = new(new());

    public BasicsTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
    }

    public static IEnumerable<object?[]> BasicTestCases()
    {
        var versions = new[] { null, "1.0", "1.1", "1.2" };

        const string data = "{ \"world\": \"world\" }";
        const string passInput = "{ \"message\": \"world\"}";
        const string failInput = "{ \"message\": \"world1\"}";

        foreach (var v in versions)
        {
            yield return new object?[]
            {
                "simple.rego",
                "example/hello",
                data,
                passInput,
                v,
                true
            };

            yield return new object?[]
            {
                "simple.rego",
                "example/hello",
                data,
                failInput,
                v,
                false
            };

            yield return new object?[]
            {
                "nodata.rego",
                "example/hello",
                null,
                passInput,
                v,
                true
            };

            yield return new object?[]
            {
                "nodata.rego",
                "example/hello",
                null,
                failInput,
                v,
                false
            };

            yield return new object?[]
            {
                "entrypoints.rego",
                null,
                null,
                passInput,
                v,
                true
            };

            yield return new object?[]
            {
                "entrypoints.rego",
                null,
                null,
                failInput,
                v,
                false
            };
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

        var compiler = new RegoCliCompiler(_options, _loggerFactory.CreateLogger<RegoCliCompiler>());
        var entrypoints = string.IsNullOrWhiteSpace(entrypoint) ? null : new[] { entrypoint };
        var policyStream = await compiler.CompileFile(Path.Combine(BasePath, source), entrypoints);
        var factory = new OpaEvaluatorFactory(loggerFactory: _loggerFactory);

        using var engine = factory.CreateWithJsonData(
            policyStream,
            data,
            options: new() { MaxAbiVersion = ver }
            );

        var result1Str = engine.EvaluateRaw(input);
        Assert.NotEmpty(result1Str);

        _output.WriteLine(result1Str);

        var result1 = JsonSerializer.Deserialize<PolicyResult[]>(result1Str);

        Assert.NotNull(result1);
        Assert.Collection(result1, p => Assert.Equal(expectedResult, p.Result));

        var result2Str = engine.EvaluateRaw(input);
        Assert.NotEmpty(result2Str);

        _output.WriteLine(result2Str);

        var result2 = JsonSerializer.Deserialize<PolicyResult[]>(result2Str);

        Assert.NotNull(result2);
        Assert.Collection(result2, p => Assert.Equal(expectedResult, p.Result));
    }

    [Theory]
    [InlineData("{ \"world\": \"world\" }", "{ \"message\": \"world\"}", true)]
    [InlineData("{ \"world\": \"world\" }", "{ \"message\": \"world1\"}", false)]
    public void SimpleRunFromCompiled(string? data, string input, bool expectedResult)
    {
        var factory = new OpaEvaluatorFactory(loggerFactory: _loggerFactory);
        using var engine = factory.CreateWithJsonData(
            File.OpenRead(Path.Combine(BasePath, "simple.wasm")),
            data
            );

        var resultStr = engine.EvaluateRaw(input);
        var result = JsonSerializer.Deserialize<PolicyResult[]>(resultStr);

        Assert.NotNull(result);
        Assert.Collection(result, p => Assert.Equal(expectedResult, p.Result));

        _output.WriteLine(resultStr);

        Assert.NotEmpty(resultStr);
    }

    private record CompositeResult
    {
        public string X { get; set; } = default!;
        public int Y { get; set; }
        public bool Z { get; set; }
    }

    [Fact]
    public async Task CompositeTests()
    {
        var opts = new WasmPolicyEngineOptions
        {
            SerializationOptions = new() { PropertyNameCaseInsensitive = true }
        };

        var compiler = new RegoCliCompiler(_options, _loggerFactory.CreateLogger<RegoCliCompiler>());
        var policyStream = await compiler.CompileFile(Path.Combine(BasePath, "composite.rego"), new[] { "example" });
        var factory = new OpaEvaluatorFactory(loggerFactory: _loggerFactory);
        using var engine = factory.CreateWithJsonData(policyStream, null, options: opts);
        var result = engine.Evaluate<object?, CompositeResult>(null, "example");

        var expected = new CompositeResult { X = "hi", Y = 1, Z = true };

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

        var factory = new OpaEvaluatorFactory(loggerFactory: _loggerFactory);

        using var engine = factory.CreateWithJsonData(
            File.OpenRead(Path.Combine(BasePath, "simple.wasm")),
            "{ \"world\": \"world\" }"
            );

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

        var factory = new OpaEvaluatorFactory(loggerFactory: _loggerFactory);

        using var engine = factory.CreateWithJsonData(
            File.OpenRead(Path.Combine(BasePath, "simple.wasm")),
            "{ \"world\": \"world\" }"
            );

        var result1 = engine.EvaluatePredicate(inp);
        Assert.Equal(expected, result1.Result);

        engine.UpdateData("{ \"world\": \"world1\" }");

        var result2 = engine.EvaluatePredicate(inp);
        Assert.Equal(expected, !result2.Result);
    }
}