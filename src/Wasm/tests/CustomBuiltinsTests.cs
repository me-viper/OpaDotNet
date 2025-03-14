﻿using System.Globalization;
using System.Text.Json.Nodes;

using OpaDotNet.InternalTesting;
using OpaDotNet.Wasm.Builtins;
using OpaDotNet.Wasm.Tests.Common;

// ReSharper disable UnusedMember.Local

namespace OpaDotNet.Wasm.Tests;

[UsedImplicitly]
[CollectionDefinition("CapabilitiesProvider", DisableParallelization = true)]
public class CapabilitiesProviderTests : CustomBuiltinsTests
{
    public CapabilitiesProviderTests(ITestOutputHelper output) : base(output)
    {
        Options = new() { CapabilitiesVersion = Utils.DefaultCapabilities };
    }

    protected override Stream Caps() => new CustomOpaImportsAbiCapabilitiesProvider().GetCapabilities();
}

[UsedImplicitly]
[CollectionDefinition("FileCapabilities", DisableParallelization = true)]
public class SimpleCustomBuiltinsTests(ITestOutputHelper output) : CustomBuiltinsTests(output)
{
    protected override Stream Caps() => File.OpenRead(Path.Combine(BasePath, "capabilities.json"));
}

public abstract class CustomBuiltinsTests(ITestOutputHelper output) : OpaTestBase(output), IAsyncLifetime
{
    private IOpaEvaluator _engine = default!;

    protected string BasePath { get; } = Path.Combine("TestData", "custom-builtins");

    private readonly Guid _outDir = Guid.NewGuid();

    private string OutPath => Path.Combine(BasePath, "out", _outDir.ToString());

    protected abstract Stream Caps();

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(OutPath);

        var policy = await CompileBundle(
            Path.Combine(BasePath, "bundle"),
            Caps(),
            p => p with
            {
                OutputPath = OutPath,
                Entrypoints =
                [
                    "custom_builtins/zero_arg",
                    "custom_builtins/one_arg",
                    "custom_builtins/one_arg_object",
                    "custom_builtins/two_arg",
                    "custom_builtins/three_arg",
                    "custom_builtins/four_arg",
                    "custom_builtins/four_arg_types",
                    "custom_builtins/valid_json",
                    "custom_builtins/json_arg",
                    "custom_builtins/memorized",
                ],
            }
            );

        var opts = WasmPolicyEngineOptions.DefaultWithJsonOptions(p => p.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
        opts.ConfigureBuiltins(
            p =>
            {
                p.DefaultBuiltins = new NotImplementedImports();
                p.CustomBuiltins.Add(new CustomOpaImportsAbi(NullLogger.Instance));
            }
            );

        _engine = OpaBundleEvaluatorFactory.Create(policy, opts);
    }

    public Task DisposeAsync()
    {
        _engine.Dispose();
        Directory.Delete(OutPath, true);
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

    [Fact]
    public void JsonArgObject()
    {
        var obj = new ArgObj { A = "A", B = 1, C = true };
        var result = _engine.Evaluate<object, string>(
            new { args = new[] { obj } },
            "custom_builtins/json_arg"
            );

        Assert.NotNull(result);

        const string expected = """{"c":true,"b":1,"a":"A"}""";
        Assert.Equal(expected, result.Result);
    }

    [Fact]
    public void Memorized()
    {
        var result = _engine.Evaluate<object, DateTime[]>(
            new(),
            "custom_builtins/memorized"
            );

        Assert.Equal(result.Result[0], result.Result[2]);
        Assert.NotEqual(result.Result[0], result.Result[1]);
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

    [OpaCustomBuiltin("custom.zeroArgBuiltin")]
    public static string ZeroArgBuiltin() => "hello";

    [OpaCustomBuiltin("custom.oneArgBuiltin")]
    public static string OneArgBuiltin(string arg1) => $"hello {arg1}";

    [OpaCustomBuiltin("custom.oneArgObjectBuiltin")]
    public static string OneArgObjectBuiltin(ArgObj arg1) => $"hello {arg1}";

    [OpaCustomBuiltin("custom.twoArgBuiltin")]
    public static string TwoArgBuiltin(string arg1, string arg2) => $"hello {arg1} {arg2}";

    [OpaCustomBuiltin("custom.threeArgBuiltin")]
    public static string ThreeArgBuiltin(string arg1, string arg2, string arg3)
        => $"hello {arg1} {arg2} {arg3}";

    [OpaCustomBuiltin("custom.fourArgBuiltin")]
    public static string FourArgBuiltin(string arg1, string arg2, string arg3, string arg4)
        => $"hello {arg1} {arg2} {arg3} {arg4}";

    [OpaCustomBuiltin("custom.fourArgTypesBuiltin")]
    public static string FourArgTypesBuiltin(double arg1, int arg2, bool? arg3, string? arg4)
        => $"hello {arg1.ToString(CultureInfo.InvariantCulture)} {arg2} {arg3} {arg4 ?? "<null>"}";

    [OpaCustomBuiltin("custom.jsonBuiltin")]
    public static string JsonBuiltin(JsonNode arg1, JsonSerializerOptions opts)
        => arg1.ToJsonString(opts ?? throw new InvalidOperationException());

    [OpaCustomBuiltin("custom.memBuiltin", Memorize = true)]
    public static DateTime DateBuiltin(string key1, int key2) => DateTime.UtcNow;
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
                },
                {
                  "name": "custom.jsonBuiltin",
                  "decl": {
                    "type": "function",
                    "args": [ { "type": "object" } ],
                    "result": { "type": "string" }
                  }
                },
                {
                  "name": "custom.memBuiltin",
                  "decl": {
                    "type": "function",
                    "args": [ { "type": "string" }, { "type": "number" } ],
                    "result": { "type": "object" }
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