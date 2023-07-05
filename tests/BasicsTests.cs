using System.Text;
using System.Text.Json.Serialization;

using JetBrains.Annotations;

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
        public bool Result { get; [UsedImplicitly] set; }
    }

    private readonly ITestOutputHelper _output;

    private readonly ILoggerFactory _loggerFactory;

    private string BasePath { get; } = Path.Combine("TestData", "basics");

    private readonly OptionsWrapper<RegoCliCompilerOptions> _options = new(new());

    public BasicsTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
    }

    public static IEnumerable<object?[]> BasicTestCases()
    {
        var versions = new[] { null, "1.0", "1.1", "1.2", "1.3" };

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
                data,
                passInput,
                v,
                true
            };

            yield return new object?[]
            {
                "entrypoints.rego",
                null,
                data,
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
        var policy = await compiler.CompileFile(Path.Combine(BasePath, source), entrypoints);
        var factory = new OpaEvaluatorFactory(loggerFactory: _loggerFactory);

        using var engine = factory.CreateFromBundle(
            policy,
            options: new() { MaxAbiVersion = ver }
            );

        engine.SetDataFromRawJson(data);

        var result1Str = engine.EvaluateRaw(input, entrypoint);
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
    public void StringData(string? data, string input, bool expectedResult)
    {
        var factory = new OpaEvaluatorFactory(loggerFactory: _loggerFactory);

        using var engine = factory.CreateFromWasm(
            File.OpenRead(Path.Combine(BasePath, "simple-1.2.wasm"))
            );

        engine.SetDataFromRawJson(data);

        var resultStr = engine.EvaluateRaw(input);
        var result = JsonSerializer.Deserialize<PolicyResult[]>(resultStr);

        Assert.NotNull(result);
        Assert.Collection(result, p => Assert.Equal(expectedResult, p.Result));

        _output.WriteLine(resultStr);

        Assert.NotEmpty(resultStr);
    }

    [Theory]
    [InlineData("{ \"world\": \"world\" }", "{ \"message\": \"world\"}", true)]
    [InlineData("{ \"world\": \"world\" }", "{ \"message\": \"world1\"}", false)]
    public Task StreamData(string data, string input, bool expectedResult)
    {
        var factory = new OpaEvaluatorFactory(loggerFactory: _loggerFactory);

        var dataStream = new MemoryStream();
        var buffer = Encoding.UTF8.GetBytes(data);
        dataStream.Write(buffer);
        dataStream.Seek(0, SeekOrigin.Begin);

        using var engine = factory.CreateFromWasm(File.OpenRead(Path.Combine(BasePath, "simple-1.2.wasm")));
        engine.SetDataFromStream(dataStream);

        var resultStr = engine.EvaluateRaw(input);
        var result = JsonSerializer.Deserialize<PolicyResult[]>(resultStr);

        Assert.NotNull(result);
        Assert.Collection(result, p => Assert.Equal(expectedResult, p.Result));

        _output.WriteLine(resultStr);

        Assert.NotEmpty(resultStr);
        return Task.CompletedTask;
    }

    private record Data([UsedImplicitly] string World);

    [Theory]
    [InlineData("{ \"message\": \"world\"}", true)]
    [InlineData("{ \"message\": \"world1\"}", false)]
    public void TypedData(string input, bool expectedResult)
    {
        var factory = new OpaEvaluatorFactory(loggerFactory: _loggerFactory);

        var opts = new WasmPolicyEngineOptions
        {
            SerializationOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            },
        };

        using var engine = factory.CreateFromWasm(
            File.OpenRead(Path.Combine(BasePath, "simple-1.2.wasm")),
            opts
            );

        engine.SetData(new Data("world"));

        var resultStr = engine.EvaluateRaw(input);
        var result = JsonSerializer.Deserialize<PolicyResult[]>(resultStr, opts.SerializationOptions);

        Assert.NotNull(result);
        Assert.Collection(result, p => Assert.Equal(expectedResult, p.Result));

        _output.WriteLine(resultStr);

        Assert.NotEmpty(resultStr);
    }

    private record CompositeResult
    {
        public string X { [UsedImplicitly] get; set; } = default!;
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

        var compiler = new RegoCliCompiler(_options, _loggerFactory.CreateLogger<RegoCliCompiler>());
        var policy = await compiler.CompileFile(Path.Combine(BasePath, "composite.rego"), new[] { "example" });
        var factory = new OpaEvaluatorFactory(loggerFactory: _loggerFactory);

        using var engine = factory.CreateFromBundle(policy, options: opts);
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

        using var engine = factory.CreateFromWasm(
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

        var factory = new OpaEvaluatorFactory(loggerFactory: _loggerFactory);

        using var engine = factory.CreateFromWasm(
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
    public void Reset()
    {
        var data = "{\"world\":\"world\"}";

        var factory = new OpaEvaluatorFactory(loggerFactory: _loggerFactory);

        using var engine = (WasmOpaEvaluator)factory.CreateFromWasm(
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