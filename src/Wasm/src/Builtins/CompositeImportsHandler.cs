using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace OpaDotNet.Wasm.Builtins;

/// <summary>
/// Handles built-in functions invocation.
/// </summary>
public sealed class CompositeImportsHandler : IOpaImportsAbi
{
    private readonly IOpaImportsAbi _default;

    private readonly IReadOnlyList<IOpaCustomBuiltins> _imports;

    private readonly ImportsCache _importsCache;

    private readonly Action<IEnumerable<string>> _print;

    private readonly ConcurrentDictionary<int, object?> _valueCache = new();

    /// <summary>
    /// Creates new instance of <see cref="OpaWasmEvaluatorFactory"/>.
    /// </summary>
    /// <param name="defaultImport">Default built-in functions implementation</param>
    /// <param name="imports">Custom built-in functions implementation</param>
    /// <param name="importsCache">Built-ins cache</param>
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

        var customPrinter = _imports.OfType<IOpaCustomPrinter>().LastOrDefault();

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

        ((IOpaImportsAbi)this).Print(strArgs);

        return null;
    }

    void IOpaImportsAbi.Print(IEnumerable<string> args) => _print(args);

    private object? CallDefault(BuiltinContext context, BuiltinArg[] args)
    {
        switch (args.Length)
        {
            case 0:
                return _default.Func(context);
            case 1:
                return _default.Func(context, args[0]);
            case 2:
                return _default.Func(context, args[0], args[1]);
            case 3:
                return _default.Func(context, args[0], args[1], args[2]);
            case 4:
                return _default.Func(context, args[0], args[1], args[2], args[3]);
        }

        _default.Abort($"Invalid number of arguments: {args.Length}");
        return null;
    }

    private object? TryCall(BuiltinContext context, BuiltinArg[] args)
    {
        var name = Name(context.FunctionName, args.Length);

        try
        {
            var func = _importsCache.TryResolveImport(_imports, name, out var attributes);

            if (func == null)
            {
                switch (name)
                {
                    case "internal.print.1":
                        return Print(args[0].As<JsonArray>(), context.JsonSerializerOptions);
                    case "trace.1":
                        ((IOpaImportsAbi)this).Print([args[0].As<string>()]);
                        return null;
                    default:
                        return CallDefault(context, args);
                }
            }

            if (attributes?.Memorize ?? false)
            {
                var argHash = 0;

                foreach (var arg in args)
                    argHash = HashCode.Combine(argHash, arg.GetArgHashCode());

                var callHash = HashCode.Combine(name, argHash);

                return _valueCache.GetOrAdd(callHash, func(args, context.JsonSerializerOptions));
            }

            return func(args, context.JsonSerializerOptions);
        }
        catch (OpaEvaluationAbortedException)
        {
            throw;
        }
        catch (OpaBuiltinException ex)
        {
            ex.Name = context.FunctionName;

            if (OnError(context, ex))
                throw;
        }
        catch (Exception ex)
        {
            if (OnError(context, ex))
                throw new OpaBuiltinException("eval_builtin_error", ex.Message, ex) { Name = context.FunctionName };
        }

        return null;
    }

    object? IOpaImportsAbi.Func(BuiltinContext context)
        => TryCall(context, []);

    object? IOpaImportsAbi.Func(BuiltinContext context, BuiltinArg arg1)
        => TryCall(context, [arg1]);

    object? IOpaImportsAbi.Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2)
        => TryCall(context, [arg1, arg2]);

    object? IOpaImportsAbi.Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3)
        => TryCall(context, [arg1, arg2, arg3]);

    object? IOpaImportsAbi.Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3, BuiltinArg arg4)
        => TryCall(context, [arg1, arg2, arg3, arg4]);

    void IOpaImportsAbi.Reset()
    {
        _valueCache.Clear();

        foreach (var import in _imports)
            import.Reset();

        _default.Reset();
    }
}