using System.Linq.Expressions;
using System.Reflection;

using OpaDotNet.Wasm.Features;

namespace OpaDotNet.Wasm;

internal class OpaImportsHandler : IOpaImportsAbi
{
    private readonly IOpaImportsAbi _default;

    private readonly IReadOnlyList<IOpaImportExtension> _imports;

    private readonly Dictionary<string, Func<BuiltinArg[], object?>> _importCache = new();

    private static readonly MethodInfo BuildArgAsMethod = typeof(BuiltinArg)
        .GetMethod(
            nameof(BuiltinArg.As),
            BindingFlags.Instance | BindingFlags.NonPublic,
            [typeof(Type), typeof(RegoValueFormat)]
            )!;

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

                if (argLen > 0)
                {
                    if (args[^1].ParameterType.IsAssignableTo(typeof(JsonSerializerOptions)))
                    {
                        passJsonOptions = true;
                        argLen -= 1;
                    }
                }

                var name = $"{attr.Name}.{argLen}";

                var instance = callable.IsStatic ? null : Expression.Constant(import);
                var argsParam = Expression.Parameter(typeof(BuiltinArg[]), "args");

                var argVars = new List<ParameterExpression>(argLen);
                var bodyBlock = new List<Expression>(argLen);

                for (var i = 0; i < argLen; i++)
                {
                    var pt = args[i].ParameterType;
                    var argVar = Expression.Variable(pt, $"arg{i}");

                    var getValFromArg = Expression.Call(
                        Expression.ArrayAccess(argsParam, Expression.Constant(i)),
                        BuildArgAsMethod,
                        Expression.Constant(pt),
                        Expression.Constant(RegoValueFormat.Json)
                        );

                    var setArg = Expression.Assign(argVar, Expression.TypeAs(getValFromArg, pt));

                    argVars.Add(argVar);
                    bodyBlock.Add(setArg);
                }

                var funcArgs = passJsonOptions
                    ? new List<Expression>([..argVars, Expression.Constant(jsonOptions)])
                    : argVars.Cast<Expression>();

                Expression call;

                if (callable.ReturnType != typeof(void))
                    call = Expression.TypeAs(Expression.Call(instance, callable, funcArgs), typeof(object));
                else
                {
                    var returnExpr = Expression.Label(Expression.Label(typeof(object)), Expression.Constant(new object()));
                    call = Expression.Block(Expression.Call(instance, callable, funcArgs), returnExpr);
                }

                bodyBlock.Add(call);

                var body = Expression.Block(argVars, bodyBlock);
                var func = Expression.Lambda<Func<BuiltinArg[], object?>>(body, argsParam).Compile();

                _importCache.TryAdd(name, func);
            }
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