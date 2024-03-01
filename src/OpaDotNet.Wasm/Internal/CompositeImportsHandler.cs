namespace OpaDotNet.Wasm.Internal;

internal class CompositeImportsHandler : IOpaImportsAbi
{
    private readonly IOpaImportsAbi _default;

    private readonly IReadOnlyList<IOpaCustomBuiltins> _imports;

    private readonly ImportsCache _importCache;

    public CompositeImportsHandler(
        IOpaImportsAbi defaultImport,
        IReadOnlyList<IOpaCustomBuiltins> imports,
        ImportsCache importCache)
    {
        ArgumentNullException.ThrowIfNull(defaultImport);
        ArgumentNullException.ThrowIfNull(imports);
        ArgumentNullException.ThrowIfNull(importCache);

        _default = defaultImport;
        _imports = imports;
        _importCache = importCache;
    }

    public void Print(IEnumerable<string> args)
    {
    }

    private static string Name(string name, int count) => $"{name}.{count}";

    private static bool OnError(BuiltinContext context, Exception ex)
    {
        if (context.StrictBuiltinErrors)
            return true;

        if (ex is NotImplementedException)
            return true;

        return false;
    }

    private bool TryCall(BuiltinContext context, BuiltinArg[] args, out object? result)
    {
        result = null;
        var name = Name(context.FunctionName, args.Length);

        try
        {
            var func = _importCache.TryResolveImport(_imports, name);

            if (func == null)
                return false;

            result = func(args);
            return true;
        }
        catch (Exception ex)
        {
            if (OnError(context, ex))
                throw;

            return true;
        }
    }

    public object? Func(BuiltinContext context)
        => TryCall(context, Array.Empty<BuiltinArg>(), out var result) ? result : _default.Func(context);

    public object? Func(BuiltinContext context, BuiltinArg arg1)
        => TryCall(context, [arg1], out var result) ? result : _default.Func(context, arg1);

    public object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2)
        => TryCall(context, [arg1, arg2], out var result) ? result : _default.Func(context, arg1, arg2);

    public object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3)
        => TryCall(context, [arg1, arg2, arg3], out var result) ? result : _default.Func(context, arg1, arg2, arg3);

    public object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3, BuiltinArg arg4)
        => TryCall(context, [arg1, arg2, arg3, arg4], out var result) ? result : _default.Func(context, arg1, arg2, arg3, arg4);

    public void Reset()
    {
        foreach (var import in _imports)
            import.Reset();

        _default.Reset();
    }
}