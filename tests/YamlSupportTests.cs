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

        _engine = OpaEvaluatorFactory.CreateFromBundle(policy);
    }

    public Task DisposeAsync()
    {
        _engine.Dispose();
        return Task.CompletedTask;
    }

    [Fact(Skip = "Yaml is not supported yet")]
    public void CanParseYaml()
    {
        var result = _engine.EvaluatePredicate<object?>(null, "yaml/support/canParseYAML");
        Assert.True(result.Result);
    }
}