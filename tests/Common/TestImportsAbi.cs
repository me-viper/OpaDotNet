using OpaDotNet.Wasm;

using Xunit.Abstractions;

namespace OpaDotNet.Tests.Common;

internal class TestImportsAbi : DefaultOpaImportsAbi
{
    private readonly ITestOutputHelper _output;

    public TestImportsAbi(ITestOutputHelper output)
    {
        _output = output;
    }

    protected override bool Trace(string message)
    {
        _output.WriteLine(message);
        return base.Trace(message);
    }

    protected override bool OnError(BuiltinContext context, Exception ex)
    {
        return true;
    }
}