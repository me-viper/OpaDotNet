using Xunit.Abstractions;

namespace OpaDotNet.Wasm.Tests.Common;

internal class TestImportsAbi(ITestOutputHelper output) : DefaultOpaImportsAbi
{
    public override void PrintLn(string message)
    {
        throw new Exception("Boom!");
    }

    public override void Print(IEnumerable<string> args)
    {
        var str = args as string[] ?? args.ToArray();
        var o = string.Join(", ", str);
        output.WriteLine(o);
        base.Print(str);
    }

    protected override bool Trace(string message)
    {
        output.WriteLine(message);
        return base.Trace(message);
    }

    protected override bool OnError(BuiltinContext context, Exception ex)
    {
        return context.StrictBuiltinErrors;
    }
}