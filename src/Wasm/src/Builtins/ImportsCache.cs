﻿using System.Linq.Expressions;
using System.Reflection;

namespace OpaDotNet.Wasm.Builtins;

/// <summary>
/// Built-ins cache.
/// </summary>
/// <param name="jsonOptions">Provides options to be used with JsonSerializer.</param>
public class ImportsCache(JsonSerializerOptions jsonOptions)
{
    private readonly object _lock = new();

    private IReadOnlyDictionary<string, ImportsCacheEntry>? _cache;

    private static readonly MethodInfo BuildArgAsMethod = typeof(BuiltinArg)
        .GetMethod(
            nameof(BuiltinArg.As),
            BindingFlags.Instance | BindingFlags.NonPublic,
            [typeof(Type), typeof(RegoValueFormat)]
            )!;

    internal void Populate(IReadOnlyList<IOpaCustomBuiltins> instances)
    {
        if (_cache == null)
        {
            lock (_lock)
            {
                if (_cache == null)
                    _cache = BuildImportsCache(instances, jsonOptions);
            }
        }
    }

    internal Func<BuiltinArg[], object?>? TryResolveImport(
        IReadOnlyList<IOpaCustomBuiltins> instances,
        string name,
        out OpaCustomBuiltinAttribute? attributes)
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

        return p => cacheItem.Import(instance, p);
    }

    private static Dictionary<string, ImportsCacheEntry> BuildImportsCache(
        IEnumerable<IOpaCustomBuiltins> imports,
        JsonSerializerOptions jsonOptions)
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

                var instanceParam = Expression.Parameter(typeof(IOpaCustomBuiltins), "instance");
                var argsParam = Expression.Parameter(typeof(BuiltinArg[]), "args");
                var instance = callable.IsStatic ? null : Expression.Convert(instanceParam, import.GetType());

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

                    var setArg = Expression.Assign(argVar, Expression.Convert(getValFromArg, pt));

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
                var func = Expression
                    .Lambda<Func<IOpaCustomBuiltins, BuiltinArg[], object?>>(body, instanceParam, argsParam)
                    .Compile();

                result[name] = new(import.GetType(), (i, a) => func(i, a), attr);
            }
        }

        return result;
    }
}