using System.Text.Json.Nodes;

using OpaDotNet.Wasm.Internal;

namespace OpaDotNet.Wasm.Builtins;

internal class CompositeImportsHandler : IOpaImportsAbi
{
    private readonly IOpaImportsAbi _default;

    private readonly IReadOnlyList<IOpaCustomBuiltins> _imports;

    private readonly ImportsCache _importsCache;

    private readonly Action<IEnumerable<string>> _print;

    public CompositeImportsHandler(
        IOpaImportsAbi defaultImport,
        IReadOnlyList<IOpaCustomBuiltins> imports,
        ImportsCache importsCache)
    {
        ArgumentNullException.ThrowIfNull(defaultImport);
        ArgumentNullException.ThrowIfNull(imports);
        ArgumentNullException.ThrowIfNull(importsCache);

        _default = defaultImport;
        _imports = imports;
        _importsCache = importsCache;
        _importsCache.Populate(_imports);

        var customPrinter = _imports.OfType<IOpaCustomPrinter>().FirstOrDefault();

        if (customPrinter != null)
            _print = p => customPrinter.Print(p);
        else
            _print = p => _default.Print(p);
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

    private object? Print(JsonArray args, JsonSerializerOptions options)
    {
        var strArgs = new List<string>();

        foreach (var arg in args)
        {
            if (arg is JsonArray ja)
            {
                if (ja.Count == 0)
                    continue;

                if (ja.Count != 1)
                    strArgs.Add(ja.ToJsonString(options));
                else
                {
                    var s = ja[0]?.ToJsonString(options);

                    if (s != null)
                        strArgs.Add(s);
                }

                continue;
            }

            var json = arg?.ToJsonString(options);

            if (json != null)
                strArgs.Add(json);
        }

        Print(strArgs);

        return null;
    }

    public void Print(IEnumerable<string> args) => _print(args);

    private bool TryCall(BuiltinContext context, BuiltinArg[] args, out object? result)
    {
        result = null;
        var name = Name(context.FunctionName, args.Length);

        try
        {
            var func = _importsCache.TryResolveImport(_imports, name);

            if (func == null)
            {
                switch (name)
                {
                    case "internal.print.1":
                        result = Print(args[0].As<JsonArray>(), context.JsonSerializerOptions);
                        return true;
                    case "trace.1":
                        Print([args[0].As<string>()]);
                        result = true;
                        return true;
                    default:
                        return false;
                }
            }

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