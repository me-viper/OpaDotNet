using System.Reflection;

namespace OpaDotNet.Wasm.Features;

/// <summary>
///
/// </summary>
internal enum OpaImportType
{
    /// <summary>
    ///
    /// </summary>
    Function,
}

/// <summary>
///
/// </summary>
/// <param name="name"></param>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal class OpaImportAttribute(string name) : Attribute
{
    /// <summary>
    ///
    /// </summary>
    public string Name { get; } = name;

    internal string? Description { get; set; }

    internal string[]? Categories { get; set; }

    /// <summary>
    ///
    /// </summary>
    internal OpaImportType Type { get; set; }
}

/// <summary>
///
/// </summary>
internal interface IOpaImportExtension
{
    /// <summary>
    ///
    /// </summary>
    void Reset();
}

internal class OpaImportsHandler : IOpaImportsAbi
{
    private readonly IOpaImportsAbi _default;

    private readonly IReadOnlyList<IOpaImportExtension> _imports;

    private readonly Dictionary<string, Func<BuiltinArg[], object?>> _importCache = new();

    public OpaImportsHandler(
        IOpaImportsAbi defaultImport,
        IEnumerable<IOpaImportExtension> imports,
        JsonSerializerOptions jsonOptions)
    {
        ArgumentNullException.ThrowIfNull(defaultImport);

        _default = defaultImport;
        _imports = imports.Reverse().ToList();
        BuildImportsCache(_imports, jsonOptions);
    }

    private void BuildImportsCache(IEnumerable<IOpaImportExtension> imports, JsonSerializerOptions jsonOptions)
    {
        foreach (var import in imports)
        {
            var callables = import.GetType().GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

            foreach (var callable in callables)
            {
                var attr = callable.GetCustomAttribute<OpaImportAttribute>();

                if (attr == null)
                    continue;

                var args = callable.GetParameters();

                if (args.Length > 5)
                {
                    throw new NotSupportedException(
                        "Imports support up to 4 arguments plus optional JsonSerializerOptions parameter"
                        );
                }

                if (args.Length == 5 && !args[4].ParameterType.IsAssignableTo(typeof(JsonSerializerOptions)))
                {
                    throw new NotSupportedException(
                        "Imports support up to 4 arguments plus optional JsonSerializerOptions parameter"
                        );
                }

                var passJsonOptions = false;
                var argLen = args.Length;

                if (args.Length > 0)
                {
                    if (args[^1].ParameterType.IsAssignableTo(typeof(JsonSerializerOptions)))
                    {
                        passJsonOptions = true;
                        argLen -= 1;
                    }
                }

                var name = $"{attr.Name}.{argLen}";
                var argMapper = Map(args[..argLen]);
                object? obj = callable.IsStatic ? null : import;

                _importCache.TryAdd(
                    name, p =>
                    {
                        var a = passJsonOptions ? [..argMapper(p), jsonOptions] : argMapper(p);
                        return callable.Invoke(obj, a);
                    }
                    );
            }
        }

        return;

        static Func<BuiltinArg[], object?[]> Map(ParameterInfo[] args)
        {
            var mapper = new Func<BuiltinArg, object?>[args.Length];

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                mapper[i] = p => p.As(arg.ParameterType);
            }

            return p =>
            {
                if (p.Length != mapper.Length)
                    throw new InvalidOperationException($"Argument count mismatch. Expected {mapper.Length}, got {p.Length}");

                var result = new object?[p.Length];

                for (var i = 0; i < p.Length; i++)
                    result[i] = mapper[i](p[i]);

                return result;
            };
        }
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
            if (!_importCache.TryGetValue(name, out var func))
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
        => TryCall(context, [], out var result) ? result : _default.Func(context);

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
    }
}