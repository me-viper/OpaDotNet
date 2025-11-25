using OpaDotNet.Wasm.Tests.Common;

namespace OpaDotNet.Wasm.Tests;

public class StringifiedSupportTests : OpaTestBase, IAsyncLifetime
{
    private IOpaEvaluator _engine = default!;

    private string BasePath { get; } = Path.Combine("TestData", "stringified-support");

    public StringifiedSupportTests(ITestOutputHelper output) : base(output)
    {
    }

    public async ValueTask InitializeAsync()
    {
        var policy = await CompileBundle(
            BasePath,
            [
                "stringified/support",
                "stringified/support/plainInputBoolean",
                "stringified/support/plainInputNumber",
                "stringified/support/plainInputString",
            ]
            );

        _engine = OpaBundleEvaluatorFactory.Create(policy);
    }

    public ValueTask DisposeAsync()
    {
        _engine.Dispose();
        return ValueTask.CompletedTask;
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