using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore.Tests.Common;

public class NotImplementedImports : IOpaImportsAbi
{
    public void Print(IEnumerable<string> args)
    {
    }

    public object? Func(BuiltinContext context) => throw new NotImplementedException();

    public object? Func(BuiltinContext context, BuiltinArg arg1) => throw new NotImplementedException();

    public object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2) => throw new NotImplementedException();

    public object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3)
        => throw new NotImplementedException();

    public object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3, BuiltinArg arg4)
        => throw new NotImplementedException();

    public void Reset()
    {
    }
}