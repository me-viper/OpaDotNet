using Microsoft.Extensions.Options;

using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class YamlSupportTests : IAsyncLifetime
{
    private IOpaEvaluator _engine = default!;

    private readonly ITestOutputHelper _output;

    private readonly ILoggerFactory _loggerFactory;

    private string BasePath { get; } = Path.Combine("TestData", "yaml");

    private readonly OptionsWrapper<RegoCliCompilerOptions> _options = new(new());

    public YamlSupportTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
    }

    public async Task InitializeAsync()
    {
        var compiler = new RegoCliCompiler(_options);
        var policy = await compiler.CompileFile(
            Path.Combine(BasePath, "yaml.rego"),
            new[]
            {
                "yaml/support/canParseYAML",
                "yaml/support/hasSyntaxError",
                "yaml/support/hasSemanticError",
                "yaml/support/hasReferenceError",
                "yaml/support/hasYAMLWarning",
                "yaml/support/canMarshalYAML",
                "yaml/support/isValidYAML",
            }
            );

        _engine = OpaEvaluatorFactory.CreateFromBundle(policy, importsAbiFactory: () => new TestImportsAbi(_output));
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
        Assert.Equal("""[{"result":[[{"foo":["1","2","3"]}]]}]""", result);
    }
    
    [Fact]
    public void IsValid()
    {
        var result = _engine.EvaluatePredicate<object?>(null, "yaml/support/isValidYAML");
        Assert.True(result.Result);
    }
}