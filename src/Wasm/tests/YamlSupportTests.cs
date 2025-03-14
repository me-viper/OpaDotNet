﻿using OpaDotNet.Wasm.Tests.Common;

namespace OpaDotNet.Wasm.Tests;

public class YamlSupportTests(ITestOutputHelper output) : OpaTestBase(output), IAsyncLifetime
{
    private IOpaEvaluator _engine = null!;

    private string BasePath { get; } = Path.Combine("TestData", "yaml");

    public async Task InitializeAsync()
    {
        var policy = await CompileFile(
            Path.Combine(BasePath, "yaml.rego"),
            [
                "yaml/support/canParseYAML",
                "yaml/support/hasSyntaxError",
                "yaml/support/hasSemanticError",
                "yaml/support/hasReferenceError",
                "yaml/support/hasYAMLWarning",
                "yaml/support/canMarshalYAML",
                "yaml/support/isValidYAML",
            ]
            );

        _engine = OpaBundleEvaluatorFactory.Create(policy);
    }

    public Task DisposeAsync()
    {
        _engine.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public void CanParseYaml()
    {
        var result = _engine.EvaluatePredicate<object?>(null, "yaml/support/canParseYAML");
        Assert.True(result.Result);
    }

    [Fact]
    public void IgnoreSyntaxError()
    {
        var result = _engine.EvaluatePredicate<object?>(null, "yaml/support/hasSyntaxError");
        Assert.False(result.Result);
    }

    [Fact]
    public void IgnoreSemanticError()
    {
        var result = _engine.EvaluatePredicate<object?>(null, "yaml/support/hasSemanticError");
        Assert.False(result.Result);
    }

    [Fact]
    public void IgnoreReferenceError()
    {
        var result = _engine.EvaluatePredicate<object?>(null, "yaml/support/hasReferenceError");
        Assert.False(result.Result);
    }

    [Fact]
    public void IgnoreWarning()
    {
        // This is NOT compatible with OPA native behaviour.
        var result = _engine.EvaluatePredicate<object?>(null, "yaml/support/hasYAMLWarning");
        Assert.True(result.Result);
    }

    [Fact]
    public void Marshal()
    {
        var result = _engine.EvaluateRaw("""[{"foo": [1, 2, 3]}]""", "yaml/support/canMarshalYAML");
        Assert.Equal("""[{"result":[[{"foo":[1,2,3]}]]}]""", result);
    }

    [Fact]
    public void IsValid()
    {
        var result = _engine.EvaluatePredicate<object?>(null, "yaml/support/isValidYAML");
        Assert.True(result.Result);
    }
}