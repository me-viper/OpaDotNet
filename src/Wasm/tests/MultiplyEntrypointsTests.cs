using OpaDotNet.Wasm.Tests.Common;

using Xunit.Abstractions;

namespace OpaDotNet.Wasm.Tests;

public class MultiplyEntrypointsTests : OpaTestBase, IAsyncLifetime
{
    private IOpaEvaluator _engine = default!;

    private string BasePath { get; } = Path.Combine("TestData", "multiple-entrypoints");

    public MultiplyEntrypointsTests(ITestOutputHelper output) : base(output)
    {
    }

    public async Task InitializeAsync()
    {
        var policy = await CompileBundle(
            BasePath,
            new[]
            {
                "example",
                "example/one",
                "example/two",
            }
            );

        _engine = OpaEvaluatorFactory.CreateFromBundle(policy);
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