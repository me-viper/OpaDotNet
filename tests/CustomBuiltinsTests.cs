using System.Globalization;

using JetBrains.Annotations;

using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

[UsedImplicitly]
public class CapabilitiesProviderTests : CustomBuiltinsTests
{
    private readonly IOpaImportsAbi _abi;

    public CapabilitiesProviderTests(ITestOutputHelper output) : base(output)
    {
        Options = new() { CapabilitiesVersion = "v0.53.0" };

        _abi = new CustomOpaImportsAbiWithCapabilitiesProvider(
            LoggerFactory.CreateLogger<CustomOpaImportsAbiWithCapabilitiesProvider>()
            );
    }

    protected override IOpaImportsAbi Create() => _abi;

    protected override Stream Caps()
    {
        if (_abi is ICapabilitiesProvider cp)
            return cp.GetCapabilities();

        throw new NotSupportedException();
    }
}

[UsedImplicitly]
public class SimpleCustomBuiltinsTests : CustomBuiltinsTests
{
    public SimpleCustomBuiltinsTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override IOpaImportsAbi Create() => new CustomOpaImportsAbi(LoggerFactory.CreateLogger<CustomOpaImportsAbi>());

    protected override Stream Caps() => File.OpenRead(Path.Combine(BasePath, "capabilities.json"));
}

public abstract class CustomBuiltinsTests : OpaTestBase, IAsyncLifetime
{
    private IOpaEvaluator _engine = default!;

    protected string BasePath { get; } = Path.Combine("TestData", "custom-builtins");

    protected CustomBuiltinsTests(ITestOutputHelper output) : base(output)
    {
    }

    protected abstract IOpaImportsAbi Create();

    protected abstract Stream Caps();

    public async Task InitializeAsync()
    {
        var policy = await CompileBundle(
            BasePath,
            new[]
            {
                "custom_builtins/zero_arg",
                "custom_builtins/one_arg",
                "custom_builtins/one_arg_object",
                "custom_builtins/two_arg",
                "custom_builtins/three_arg",
                "custom_builtins/four_arg",
                "custom_builtins/four_arg_types",
                "custom_builtins/valid_json",
            },
            Caps()
            );

        var factory = new OpaBundleEvaluatorFactory(
            policy,
            importsAbiFactory: Create,
            loggerFactory: LoggerFactory
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

file class CustomOpaImportsAbi : DefaultOpaImportsAbi
{
    private readonly ILogger _logger;

    public CustomOpaImportsAbi(ILogger<CustomOpaImportsAbi> logger)
    {
        _logger = logger;
    }

    public override void PrintLn(string message)
    {
        _logger.LogDebug("{Message}", message);
    }

    public override object? Func(BuiltinContext context)
    {
        if (string.Equals("custom.zeroArgBuiltin", context.FunctionName, StringComparison.Ordinal))
            return "hello";

        if (string.Equals("json.valid_json", context.FunctionName, StringComparison.Ordinal))
            throw new Exception("Should never happen");

        return base.Func(context);
    }

    public override object? Func(BuiltinContext context, BuiltinArg arg1)
    {
        if (string.Equals("custom.oneArgBuiltin", context.FunctionName, StringComparison.Ordinal))
            return $"hello {arg1.AsOrNull<string>()}";

        if (string.Equals("custom.oneArgObjectBuiltin", context.FunctionName, StringComparison.Ordinal))
            return $"hello {arg1.AsOrNull<ArgObj>()}";

        return base.Func(context, arg1);
    }

    public override object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2)
    {
        if (string.Equals("custom.twoArgBuiltin", context.FunctionName, StringComparison.Ordinal))
            return $"hello {arg1.AsOrNull<string>()} {arg2.AsOrNull<string>()}";

        return base.Func(context, arg1, arg2);
    }

    public override object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3)
    {
        if (string.Equals("custom.threeArgBuiltin", context.FunctionName, StringComparison.Ordinal))
            return $"hello {arg1.AsOrNull<string>()} {arg2.AsOrNull<string>()} {arg3.AsOrNull<string>()}";

        return base.Func(context, arg1, arg2, arg3);
    }

    public override object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3, BuiltinArg arg4)
    {
        if (string.Equals("custom.fourArgBuiltin", context.FunctionName, StringComparison.Ordinal))
            return $"hello {arg1.AsOrNull<string>()} {arg2.AsOrNull<string>()} {arg3.AsOrNull<string>()} {arg4.AsOrNull<string>()}";

        if (string.Equals("custom.fourArgTypesBuiltin", context.FunctionName, StringComparison.Ordinal))
        {
            return $"hello {arg1.AsOrNull<double>().ToString(CultureInfo.InvariantCulture)} " +
                $"{arg2.As<int>()} {arg3.AsOrNull<bool>()} {arg4.AsOrNull<string?>() ?? "<null>"}";
        }

        return base.Func(context, arg1, arg2, arg3, arg4);
    }
}

file class CustomOpaImportsAbiWithCapabilitiesProvider : CustomOpaImportsAbi, ICapabilitiesProvider
{
    public CustomOpaImportsAbiWithCapabilitiesProvider(ILogger<CustomOpaImportsAbiWithCapabilitiesProvider> logger) : base(logger)
    {
    }

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