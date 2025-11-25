using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace OpaDotNet.Benchmarks;

[Config(typeof(Config))]

//[DisassemblyDiagnoser(printSource: true, maxDepth: 2)]
//[DryJob]
public class CallPerf
{
    private static readonly CX _target = new();

    private const string Arg = "perf";

    private static readonly MethodInfo Method = typeof(CX).GetMethod(nameof(CX.Do))!;

    public CallPerf()
    {
        _methodDelegate = (Func<string, string>)Delegate.CreateDelegate(
            typeof(Func<string, string>),
            _target,
            Method
            );

        var instance = Expression.Constant(_target);
        var param = Expression.Parameter(typeof(string), "arg");
        var call = Expression.Call(instance, Method, param);
        var func = Expression.Lambda<Func<string, string>>(call, param);
        _methodExprTree = func.Compile();
    }

    [Benchmark(Baseline = true)]
    public object DirectCall()
    {
        return _target.Do(Arg);
    }

    private readonly Func<string, string> _methodDelegate;

    [Benchmark]
    public object DelegateCall()
    {
        return _methodDelegate(Arg);
    }

    [Benchmark]
    public object ReflectionCall()
    {
        return Method.Invoke(_target, [Arg])!;
    }

    private readonly Func<string, string> _methodExprTree;

    [Benchmark]
    public object ExpressionTreeCall()
    {
        return _methodExprTree(Arg);
    }
}

internal class CX
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public string Do(string arg) => $"Do {arg}";
}