﻿using OpaDotNet.InternalTesting;
using OpaDotNet.Wasm.Rego;
using OpaDotNet.Wasm.Tests.Common;

namespace OpaDotNet.Wasm.Tests;

public class SerializationTests : OpaTestBase, IAsyncLifetime
{
    private IOpaEvaluator _engine = default!;

    public SerializationTests(ITestOutputHelper output) : base(output)
    {
        Options = new() { CapabilitiesVersion = Utils.DefaultCapabilities };
    }

    public async Task InitializeAsync()
    {
        var policy = await CompileBundle(
            Path.Combine("TestData", "serialization"),
            [
                "serialization/isArray",
                "serialization/isSet",
                "serialization/retArray",
                "serialization/retSet",
                "serialization/charEncoding",
            ],
            Path.Combine("TestData", "serialization", "capabilities.json")
            );

        var logger = LoggerFactory.CreateLogger<SerializationImports>();
        var opts = WasmPolicyEngineOptions.Default;
        opts.ConfigureBuiltins(p => p.DefaultBuiltins = new SerializationImports(logger));

        _engine = OpaBundleEvaluatorFactory.Create(policy, opts);
    }

    public Task DisposeAsync()
    {
        _engine.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public void Array()
    {
        var result = _engine.EvaluatePredicate<object?>(null, "serialization/isArray");
        Assert.True(result.Result);
    }

    [Fact]
    public void ReturnsArray()
    {
        var result = _engine.EvaluatePredicate<object?>(null, "serialization/retArray");
        Assert.True(result.Result);
    }

    [Fact]
    public void Set()
    {
        var result = _engine.EvaluatePredicate<object?>(null, "serialization/isSet");
        Assert.True(result.Result);
    }

    [Fact]
    public void ReturnsSet()
    {
        var result = _engine.EvaluatePredicate<object?>(null, "serialization/retSet");
        Assert.True(result.Result);
    }

    [Fact]
    public void CharEncoding()
    {
        var result = _engine.EvaluatePredicate(new { s = "?a=1&b=x" }, "serialization/charEncoding");
        Assert.True(result.Result);
    }

    private class SerializationImports : DefaultOpaImportsAbi
    {
        private readonly ILogger _logger;

        public SerializationImports(ILogger<SerializationImports> logger)
        {
            _logger = logger;
        }

        public override void Print(IEnumerable<string> args)
        {
            _logger.LogDebug("{Message}", string.Join(';', args));
        }

        public override object? Func(BuiltinContext context)
        {
            if (string.Equals(context.FunctionName, "custom.set", StringComparison.Ordinal))
                return new RegoSet<string>(["1", "2"]);

            if (string.Equals(context.FunctionName, "custom.array", StringComparison.Ordinal))
                return new[] { "1", "2" };

            return base.Func(context);
        }

        public override object? Func(BuiltinContext context, BuiltinArg arg1)
        {
            if (string.Equals(context.FunctionName, "custom.setOrArray", StringComparison.Ordinal))
            {
                if (arg1.Raw.TryGetRegoSet<string>(out var set, context.JsonSerializerOptions))
                    return set;

                return arg1.Raw;
            }

            return base.Func(context, arg1);
        }
    }
}