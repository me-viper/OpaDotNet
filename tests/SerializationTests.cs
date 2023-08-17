﻿using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Rego;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class SerializationTests : OpaTestBase, IAsyncLifetime
{
    private IOpaEvaluator _engine = default!;

    public SerializationTests(ITestOutputHelper output) : base(output)
    {
    }

    public async Task InitializeAsync()
    {
        var policy = await CompileBundle(
            Path.Combine("TestData", "serialization"),
            new[]
            {
                "serialization/isArray",
                "serialization/isSet",
                "serialization/retArray",
                "serialization/retSet",
            },
            Path.Combine("TestData", "serialization", "capabilities.json")
            );

        _engine = OpaEvaluatorFactory.CreateFromBundle(policy, importsAbiFactory: () => new SerializationImports());
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

    private class SerializationImports : DefaultOpaImportsAbi
    {
        public override object? Func(BuiltinContext context)
        {
            if (string.Equals(context.FunctionName, "custom.set", StringComparison.Ordinal))
                return new RegoSet<string>(new[] { "1", "2" });

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