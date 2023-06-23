using System.Globalization;

using Microsoft.Extensions.Options;

using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class CustomBuiltinsTests : IAsyncLifetime
{
    private IOpaEvaluator _engine = default!;

    private readonly ILoggerFactory _loggerFactory;

    private string BasePath { get; } = Path.Combine("TestData", "custom-builtins");

    public CustomBuiltinsTests(ITestOutputHelper output)
    {
        _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
    }

    public async Task InitializeAsync()
    {
        var options = new OptionsWrapper<RegoCliCompilerOptions>(new());
        var compiler = new RegoCliCompiler(options, _loggerFactory.CreateLogger<RegoCliCompiler>());
        var policy = await compiler.CompileBundle(
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
            Path.Combine(BasePath, "capabilities.json")
            );

        var factory = new OpaEvaluatorFactory(
            importsAbi: new CustomOpaImportsAbi(_loggerFactory.CreateLogger<CustomOpaImportsAbi>()),
            loggerFactory: _loggerFactory
            );

        _engine = factory.CreateFromBundle(policy);
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

    private record ArgObj
    {
        public string? A { get; set; }

        public int B { get; set; }

        public bool C { get; set; }
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

    private class CustomOpaImportsAbi : DefaultOpaImportsAbi
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

        public override object Func(BuiltinContext context)
        {
            if (string.Equals("custom.zeroArgBuiltin", context.FunctionName, StringComparison.Ordinal))
                return "hello";

            if (string.Equals("json.valid_json", context.FunctionName, StringComparison.Ordinal))
                throw new Exception("Should never happen");

            return base.Func(context);
        }

        public override object Func(BuiltinContext context, BuiltinArg arg1)
        {
            if (string.Equals("custom.oneArgBuiltin", context.FunctionName, StringComparison.Ordinal))
                return $"hello {arg1.AsOrNull<string>()}";

            if (string.Equals("custom.oneArgObjectBuiltin", context.FunctionName, StringComparison.Ordinal))
                return $"hello {arg1.AsOrNull<ArgObj>()}";

            return base.Func(context, arg1);
        }

        public override object Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2)
        {
            if (string.Equals("custom.twoArgBuiltin", context.FunctionName, StringComparison.Ordinal))
                return $"hello {arg1.AsOrNull<string>()} {arg2.AsOrNull<string>()}";

            return base.Func(context, arg1, arg2);
        }

        public override object Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3)
        {
            if (string.Equals("custom.threeArgBuiltin", context.FunctionName, StringComparison.Ordinal))
                return $"hello {arg1.AsOrNull<string>()} {arg2.AsOrNull<string>()} {arg3.AsOrNull<string>()}";

            return base.Func(context, arg1, arg2, arg3);
        }

        public override object Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3, BuiltinArg arg4)
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
}