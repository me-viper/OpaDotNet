using System.Globalization;

using JetBrains.Annotations;

using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;

using Xunit.Abstractions;

// ReSharper disable UnusedMember.Local

namespace OpaDotNet.Tests;

[UsedImplicitly]
public class CapabilitiesProviderTests(ITestOutputHelper output) : CustomBuiltinsTests(output)
{
    protected override Stream Caps() => new CustomOpaImportsAbiCapabilitiesProvider().GetCapabilities();
}

[UsedImplicitly]
public class SimpleCustomBuiltinsTests(ITestOutputHelper output) : CustomBuiltinsTests(output)
{
    protected override Stream Caps() => File.OpenRead(Path.Combine(BasePath, "capabilities.json"));
}

public abstract class CustomBuiltinsTests(ITestOutputHelper output) : OpaTestBase(output), IAsyncLifetime
{
    private IOpaEvaluator _engine = default!;

    protected string BasePath { get; } = Path.Combine("TestData", "custom-builtins");

    protected abstract Stream Caps();

    public async Task InitializeAsync()
    {
        var policy = await CompileBundle(
            BasePath,
            [
                "custom_builtins/zero_arg",
                "custom_builtins/one_arg",
                "custom_builtins/one_arg_object",
                "custom_builtins/two_arg",
                "custom_builtins/three_arg",
                "custom_builtins/four_arg",
                "custom_builtins/four_arg_types",
                "custom_builtins/valid_json",
            ],
            Caps()
            );

        var factory = new OpaBundleEvaluatorFactory(
            policy,
            importsAbiFactory: () => new NotImplementedImports(),
            loggerFactory: LoggerFactory,
            options: new() { CustomBuiltins = { () => new CustomOpaImportsAbi(NullLogger.Instance) }}
            );

        _engine = factory.Create();
    }

    public Task DisposeAsync()
    {
        _engine.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public void ZeroArg()
    {
        var result = _engine.Evaluate<object, string>(new object(), "custom_builtins/zero_arg");

        Assert.NotNull(result);
        Assert.Equal("hello", result.Result);
    }

    [Fact]
    public void OneArg()
    {
        var result = _engine.Evaluate<object, string>(
            new { args = new[] { "arg0" } },
            "custom_builtins/one_arg"
            );

        Assert.NotNull(result);
        Assert.Equal("hello arg0", result.Result);
    }

    [Fact]
    public void OneArgObject()
    {
        var obj = new ArgObj { A = "A", B = 1, C = true };
        var result = _engine.Evaluate<object, string>(
            new { args = new[] { obj } },
            "custom_builtins/one_arg_object"
            );

        Assert.NotNull(result);
        Assert.Equal($"hello {obj}", result.Result);
    }

    [Fact]
    public void TwoArg()
    {
        var result = _engine.Evaluate<object, string>(
            new { args = new[] { "arg0", "arg1" } },
            "custom_builtins/two_arg"
            );

        Assert.NotNull(result);
        Assert.Equal("hello arg0 arg1", result.Result);
    }

    [Fact]
    public void ThreeArg()
    {
        var result = _engine.Evaluate<object, string>(
            new { args = new[] { "arg0", "arg1", "arg2" } },
            "custom_builtins/three_arg"
            );

        Assert.NotNull(result);
        Assert.Equal("hello arg0 arg1 arg2", result.Result);
    }

    [Fact]
    public void FourArg()
    {
        var result = _engine.Evaluate<object, string>(
            new { args = new[] { "arg0", "arg1", "arg2", "arg3" } },
            "custom_builtins/four_arg"
            );

        Assert.NotNull(result);
        Assert.Equal("hello arg0 arg1 arg2 arg3", result.Result);
    }

    [Fact]
    public void FourArgTypes()
    {
        var result = _engine.Evaluate<object, string>(
            new { args = new object?[] { 5.7, 2, true, null } },
            "custom_builtins/four_arg_types"
            );

        Assert.NotNull(result);
        Assert.Equal("hello 5.7 2 True <null>", result.Result);
    }

    [Fact]
    public void BuiltinOverride()
    {
        var resultString = _engine.EvaluateRaw(
            "{}",
            "custom_builtins/valid_json"
            );

        var result = JsonSerializer.Deserialize<PolicyEvaluationResult<bool>[]>(resultString);
        Assert.NotNull(result);
        Assert.Collection(result, p => Assert.True(p.Result));
    }
}

file record ArgObj
{
    [UsedImplicitly]
    public string? A { get; set; }

    [UsedImplicitly]
    public int B { get; set; }

    [UsedImplicitly]
    public bool C { get; set; }
}

file class CustomOpaImportsAbi(ILogger logger) : IOpaCustomBuiltins
{
    public void Reset() => logger.LogDebug("Reset");

    [OpaImport("custom.zeroArgBuiltin")]
    public static string ZeroArgBuiltin() => "hello";

    [OpaImport("custom.oneArgBuiltin")]
    public static string OneArgBuiltin(string arg1) => $"hello {arg1}";

    [OpaImport("custom.oneArgObjectBuiltin")]
    public static string OneArgObjectBuiltin(ArgObj arg1) => $"hello {arg1}";

    [OpaImport("custom.twoArgBuiltin")]
    public static string TwoArgBuiltin(string arg1, string arg2) => $"hello {arg1} {arg2}";

    [OpaImport("custom.threeArgBuiltin")]
    public static string ThreeArgBuiltin(string arg1, string arg2, string arg3)
        => $"hello {arg1} {arg2} {arg3}";

    [OpaImport("custom.fourArgBuiltin")]
    public static string FourArgBuiltin(string arg1, string arg2, string arg3, string arg4)
        => $"hello {arg1} {arg2} {arg3} {arg4}";

    [OpaImport("custom.fourArgTypesBuiltin")]
    public static string FourArgTypesBuiltin(double arg1, int arg2, bool? arg3, string? arg4)
        => $"hello {arg1.ToString(CultureInfo.InvariantCulture)} {arg2} {arg3} {arg4 ?? "<null>"}";
}

file class CustomOpaImportsAbiCapabilitiesProvider : ICapabilitiesProvider
{
    public Stream GetCapabilities()
    {
        var caps = """
            {
              "builtins": [
                {
                  "name": "custom.zeroArgBuiltin",
                  "decl": {
                    "type": "function",
                    "args": [],
                    "result": { "type": "string" }
                  }
                },
                {
                  "name": "custom.oneArgBuiltin",
                  "decl": {
                    "type": "function",
                    "args": [ { "type": "string" } ],
                    "result": { "type": "string" }
                  }
                },
                {
                  "name": "custom.oneArgObjectBuiltin",
                  "decl": {
                    "type": "function",
                    "args": [ { "type": "object" } ],
                    "result": { "type": "string" }
                  }
                },
                {
                  "name": "custom.twoArgBuiltin",
                  "decl": {
                    "type": "function",
                    "args": [ { "type": "string" }, { "type": "string" } ],
                    "result": { "type": "string" }
                  }
                },
                {
                  "name": "custom.threeArgBuiltin",
                  "decl": {
                    "type": "function",
                    "args": [ { "type": "string" }, { "type": "string" }, { "type": "string" } ],
                    "result": { "type": "string" }
                  }
                },
                {
                  "name": "custom.fourArgBuiltin",
                  "decl": {
                    "type": "function",
                    "args": [ { "type": "string" }, { "type": "string" }, { "type": "string" }, { "type": "string" } ],
                    "result": { "type": "string" }
                  }
                },
                {
                  "name": "custom.fourArgTypesBuiltin",
                  "decl": {
                    "type": "function",
                    "args": [ { "type": "number" }, { "type": "number" }, { "type": "boolean" }, { "type": "null" } ],
                    "result": { "type": "string" }
                  }
                },
                {
                  "name": "json.is_valid",
                  "decl": {
                    "type": "function",
                    "args": [ { "type": "string" } ],
                    "result": { "type": "boolean" }
                  }
                }
                ]
            }
            """u8;

        var ms = new MemoryStream();
        ms.Write(caps);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}