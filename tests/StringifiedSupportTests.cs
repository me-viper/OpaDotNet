using Microsoft.Extensions.Options;

using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class StringifiedSupportTests : IAsyncLifetime
{
    private IOpaEvaluator _engine = default!;

    private readonly ILoggerFactory _loggerFactory;

    private readonly OptionsWrapper<RegoCliCompilerOptions> _options = new(new());

    private string BasePath { get; } = Path.Combine("TestData", "stringified-support");

    public StringifiedSupportTests(ITestOutputHelper output)
    {
        _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
    }

    public async Task InitializeAsync()
    {
        var compiler = new RegoCliCompiler(_options, _loggerFactory.CreateLogger<RegoCliCompiler>());
        var policy = await compiler.CompileBundle(
            BasePath,
            new[]
            {
                "stringified/support",
                "stringified/support/plainInputBoolean",
                "stringified/support/plainInputNumber",
                "stringified/support/plainInputString",
            }
            );

        var factory = new OpaEvaluatorFactory(loggerFactory: _loggerFactory);
        _engine = factory.CreateFromBundle(policy);
    }

    public Task DisposeAsync()
    {
        _engine.Dispose();
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData("test", true)]
    [InlineData("invalid", false)]
    public void StringInput(string input, bool expected)
    {
        var resultPos = _engine.Evaluate<string, bool>(input, "stringified/support/plainInputString");
        Assert.NotNull(resultPos);
        Assert.Equal(expected, resultPos.Result);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void BoolInput(bool input, bool expected)
    {
        var resultPos = _engine.Evaluate<bool, bool>(input, "stringified/support/plainInputBoolean");
        Assert.NotNull(resultPos);
        Assert.Equal(expected, resultPos.Result);
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void NumberInput(int input, bool expected)
    {
        var resultPos = _engine.Evaluate<int, bool>(input, "stringified/support/plainInputNumber");
        Assert.NotNull(resultPos);
        Assert.Equal(expected, resultPos.Result);
    }
}