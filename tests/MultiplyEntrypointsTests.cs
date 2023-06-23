using Microsoft.Extensions.Options;

using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class MultiplyEntrypointsTests : IAsyncLifetime
{
    private IOpaEvaluator _engine = default!;

    private readonly ILoggerFactory _loggerFactory;

    private string BasePath { get; } = Path.Combine("TestData", "multiple-entrypoints");

    public MultiplyEntrypointsTests(ITestOutputHelper output)
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
                "example",
                "example/one",
                "example/two",
            });

        var factory = new OpaEvaluatorFactory(loggerFactory: _loggerFactory);
        _engine = factory.CreateFromBundle(policy);
    }

    public Task DisposeAsync()
    {
        _engine.Dispose();
        return Task.CompletedTask;
    }

    private record CompositeResult
    {
        public bool MyRule { get; set; }
        public bool MyOtherRule { get; set; }
    }

    [Fact]
    public void DefaultEndpoint()
    {
        var result = _engine.Evaluate<object?, CompositeResult>(null);
        var expected = new CompositeResult { MyRule = false, MyOtherRule = false };

        Assert.NotNull(result);
        Assert.Equal(expected, result.Result);
    }

    [Theory]
    [InlineData("example/one")]
    [InlineData("example/two")]
    public void NamedEndpoint(string name)
    {
        var result = _engine.Evaluate<object?, CompositeResult>(null, name);
        var expected = new CompositeResult { MyRule = false, MyOtherRule = false };

        Assert.NotNull(result);
        Assert.Equal(expected, result.Result);
    }

    [Fact]
    public void EndpointDoesNotExist()
    {
        Assert.Throws<OpaEvaluationException>(() => _engine.Evaluate<object?, CompositeResult>(null, "does-not-exist"));
    }
}