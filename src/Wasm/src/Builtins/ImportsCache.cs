using System.Linq.Expressions;
using System.Reflection;

namespace OpaDotNet.Wasm.Builtins;

internal class CustomBuiltinInfo
{
    public bool Memorize { get; set; }

    public bool IsAsync { get; set; }
}

/// <summary>
/// Built-ins cache.
/// </summary>
internal sealed class ImportsCache
{
    private readonly object _lock = new();

    private volatile IReadOnlyDictionary<string, ImportsCacheEntry>? _cache;

    private static readonly MethodInfo BuildArgAsMethod = typeof(BuiltinArg)
        .GetMethod(
            nameof(BuiltinArg.As),
            BindingFlags.Instance | BindingFlags.NonPublic,
            [typeof(Type), typeof(RegoValueFormat)]
            )!;

    private static readonly MethodInfo TaskFromResultMethod = typeof(Task)
        .GetMethod(nameof(Task.FromResult), BindingFlags.Static | BindingFlags.Public)!
        .MakeGenericMethod(typeof(object));

    private static readonly MethodInfo AwaitTaskMethod = typeof(ImportsCache)
        .GetMethod(nameof(AwaitTaskAsync), BindingFlags.Static | BindingFlags.NonPublic, [typeof(Task)])!;

    private static readonly MethodInfo AwaitTaskOfTMethod = typeof(ImportsCache)
        .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
        .Single(p => p.Name == nameof(AwaitTaskAsync) && p.IsGenericMethodDefinition);

    private static readonly MethodInfo TaskRunMethod = typeof(Task)
        .GetMethod(
            nameof(Task.Run),
            BindingFlags.Public | BindingFlags.Static,
            null,
            [typeof(Func<>).MakeGenericType(Type.MakeGenericMethodParameter(0))],
            null
            )!
        .MakeGenericMethod(typeof(object));

    internal void Populate(IReadOnlyList<IOpaCustomBuiltins> instances, bool isAsync)
    {
        if (_cache == null)
        {
            lock (_lock)
            {
                _cache ??= BuildImportsCache(instances, isAsync);
            }
        }
    }

    internal Func<BuiltinArg[], IOpaCustomBuiltinsContext, Task<object?>>? TryResolveImport(
        IReadOnlyList<IOpaCustomBuiltins> instances,
        string name,
        out CustomBuiltinInfo? attributes)
    {
        attributes = null;

        if (instances.Count == 0)
            return null;

        if (_cache == null)
            return null;

        if (!_cache.TryGetValue(name, out var cacheItem))
            return null;

        var instance = instances.FirstOrDefault(p => p.GetType() == cacheItem.Type);

        if (instance == null)
            return null;

        attributes = cacheItem.Attributes;

        return (args, opts) => cacheItem.Import(instance, args, opts);
    }

    private static Dictionary<string, ImportsCacheEntry> BuildImportsCache(
        IEnumerable<IOpaCustomBuiltins> imports,
        bool isAsync)
    {
        var result = new Dictionary<string, ImportsCacheEntry>();

        foreach (var import in imports)
        {
            var callables = import.GetType().GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

            foreach (var callable in callables)
            {
                var attr = callable.GetCustomAttribute<OpaCustomBuiltinAttribute>();

                if (attr == null)
                    continue;

                if (callable.IsGenericMethod)
                    throw new NotSupportedException("Generic built-ins are not supported");

                var args = callable.GetParameters();

                if (args.Length > 5)
                {
                    throw new NotSupportedException(
                        "Imports support up to 4 arguments plus optional JsonSerializerOptions or ICustomBuiltinContext parameter"
                        );
                }

                if (args.Length == 5)
                {
                    var validParam = args[4].ParameterType.IsAssignableTo(typeof(JsonSerializerOptions))
                        || args[4].ParameterType.IsAssignableTo(typeof(IOpaCustomBuiltinsContext));

                    if (!validParam)
                    {
                        throw new NotSupportedException(
                            "Imports support up to 4 arguments plus optional JsonSerializerOptions or ICustomBuiltinContext parameter"
                            );
                    }
                }

                var passJsonOptions = false;
                var passContext = false;
                var argLen = args.Length;

                if (argLen > 0)
                {
                    if (args[^1].ParameterType.IsAssignableTo(typeof(JsonSerializerOptions)))
                    {
                        passJsonOptions = true;
                        argLen -= 1;
                    }

                    if (args[^1].ParameterType.IsAssignableTo(typeof(IOpaCustomBuiltinsContext)))
                    {
                        passContext = true;
                        argLen -= 1;
                    }

                    if ((passJsonOptions || passContext) && argLen > 0)
                    {
                        if (args[argLen - 1].ParameterType.IsAssignableTo(typeof(JsonSerializerOptions))
                            || args[argLen - 1].ParameterType.IsAssignableTo(typeof(IOpaCustomBuiltinsContext)))
                        {
                            throw new NotSupportedException(
                                "Imports support up to 4 arguments plus optional JsonSerializerOptions or ICustomBuiltinContext parameter"
                                );
                        }
                    }
                }

                var name = $"{attr.Name}.{argLen}";

                var instanceParam = Expression.Parameter(typeof(IOpaCustomBuiltins), "instance");
                var argsParam = Expression.Parameter(typeof(BuiltinArg[]), "args");
                var contextParam = Expression.Parameter(typeof(IOpaCustomBuiltinsContext), "context");
                var instance = callable.IsStatic ? null : Expression.Convert(instanceParam, import.GetType());

                var argVars = new List<ParameterExpression>(args.Length);
                var bodyBlock = new List<Expression>(args.Length);

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

                    var setArg = Expression.Assign(argVar, Expression.Convert(getValFromArg, pt));

                    argVars.Add(argVar);
                    bodyBlock.Add(setArg);
                }

                if (passJsonOptions)
                {
                    var jsonVar = Expression.Variable(typeof(JsonSerializerOptions), "argJsonOpts");
                    var setJsonArg = Expression.Assign(
                        jsonVar,
                        Expression.Property(
                            contextParam,
                            typeof(IOpaCustomBuiltinsContext),
                            nameof(IOpaCustomBuiltinsContext.JsonSerializerOptions)
                            )
                        );

                    argVars.Add(jsonVar);
                    bodyBlock.Add(setJsonArg);
                }

                if (passContext)
                {
                    var contextVar = Expression.Variable(typeof(IOpaCustomBuiltinsContext), "argContext");
                    var setContextArg = Expression.Assign(contextVar, contextParam);

                    argVars.Add(contextVar);
                    bodyBlock.Add(setContextArg);
                }

                var funcArgs = argVars.Cast<Expression>();

                var isAsyncFunc = false;
                Expression call;

                // TODO: Handle ValueTask?
                if (callable.ReturnType == typeof(ValueTask))
                    throw new NotSupportedException("Built-ins returning ValueTask or ValueTask<T> are not supported");

                if (callable.ReturnType.IsGenericType && callable.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                    throw new NotSupportedException("Built-ins returning ValueTask or ValueTask<T> are not supported");

                if (callable.ReturnType == typeof(Task))
                {
                    // AwaitTaskAsync(X());
                    bodyBlock.Add(Expression.Call(AwaitTaskMethod, Expression.Call(instance, callable, funcArgs)));
                    isAsyncFunc = true;
                }
                else if (GetTaskResultType(callable.ReturnType) is { } taskResultType)
                {
                    // AwaitTaskAsync<T>(X());
                    bodyBlock.Add(
                        Expression.Call(
                            AwaitTaskOfTMethod.MakeGenericMethod(taskResultType),
                            Expression.Call(instance, callable, funcArgs)
                            )
                        );
                    isAsyncFunc = true;
                }
                else if (isAsync)
                {
                    if (callable.ReturnType != typeof(void))
                        call = Expression.TypeAs(Expression.Call(instance, callable, funcArgs), typeof(object));
                    else
                    {
                        var returnExpr = Expression.Label(Expression.Label(typeof(object)), Expression.Constant(new object()));
                        call = Expression.Block(Expression.Call(instance, callable, funcArgs), returnExpr);
                    }

                    // Inner lambda: () => X()
                    var innerLambda = Expression.Lambda<Func<object>>(call);

                    // Call: Task.Run<object>(() => X())
                    var callExpr = Expression.Call(TaskRunMethod, innerLambda);

                    bodyBlock.Add(callExpr);
                    isAsyncFunc = true;
                }
                else
                {
                    if (callable.ReturnType != typeof(void))
                        call = Expression.TypeAs(Expression.Call(instance, callable, funcArgs), typeof(object));
                    else
                    {
                        var returnExpr = Expression.Label(Expression.Label(typeof(object)), Expression.Constant(new object()));
                        call = Expression.Block(Expression.Call(instance, callable, funcArgs), returnExpr);
                    }

                    var resultVarExpr = Expression.Variable(typeof(object), "result");
                    var retValue = Expression.Block(
                        [resultVarExpr],
                        Expression.Assign(resultVarExpr, call),
                        Expression.Call(TaskFromResultMethod, resultVarExpr)
                        );

                    // result = X();
                    // Task.FromResult(result);
                    bodyBlock.Add(retValue);
                }

                var body = Expression.Block(argVars, bodyBlock);
                var func = Expression
                    .Lambda<Func<IOpaCustomBuiltins, BuiltinArg[], IOpaCustomBuiltinsContext, Task<object?>>>(
                        body,
                        instanceParam,
                        argsParam,
                        contextParam
                        )
                    .Compile();

                var cbi = new CustomBuiltinInfo
                {
                    Memorize = attr.Memorize,
                    IsAsync = isAsyncFunc,
                };

                result[name] = new(import.GetType(), func, cbi);
            }
        }

        return result;
    }

    private static Type? GetTaskResultType(Type type)
    {
        for (var t = type; t != null; t = t.BaseType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>))
                return t.GetGenericArguments()[0];
        }

        return null;
    }

    // Awaiting (instead of ContinueWith) keeps the built-in's own exception and cancellation state
    // instead of wrapping them into AggregateException or swallowing cancellation.
    private static async Task<object?> AwaitTaskAsync(Task task)
    {
        await task.ConfigureAwait(false);
        return new object();
    }

    private static async Task<object?> AwaitTaskAsync<T>(Task<T> task) => await task.ConfigureAwait(false);
}