using System.Collections.ObjectModel;

using OpaDotNet.Wasm.Builtins;

using Xunit.Abstractions;

namespace OpaDotNet.Wasm.Tests.Common;

internal class TestImportsAbi(ITestOutputHelper output) : DefaultOpaImportsAbi
{
    public override void Print(IEnumerable<string> args)
    {
        var str = args as string[] ?? args.ToArray();
        var o = string.Join(", ", str);
        output.WriteLine(o);
        base.Print(str);
    }

    protected override bool OnError(BuiltinContext context, Exception ex)
    {
        return context.StrictBuiltinErrors;
    }
}

internal class TestBuiltinsFactory(ITestOutputHelper output) : IBuiltinsFactory
{
    public IReadOnlyList<Func<IOpaCustomBuiltins>> CustomBuiltins { get; init; } = [];

    public IOpaImportsAbi Create()
    {
        return new CompositeImportsHandler(
            new TestImportsAbi(output),
            CustomBuiltins.Select(p => p()).ToList(),
            new ImportsCache(JsonSerializerOptions.Default)
            );
    }
}