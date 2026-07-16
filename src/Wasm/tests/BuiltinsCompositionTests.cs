using OpaDotNet.Wasm.Builtins;
using OpaDotNet.Wasm.Tests.Common;

namespace OpaDotNet.Wasm.Tests;

public class BuiltinsCompositionTests : OpaTestBase
{
    private readonly IOpaImportsAbi _imports;

    private readonly IOpaImportsAbi _default = new DefaultOpaImportsAbi();

    private readonly IOpaCustomBuiltins _ext1 = new Ext(NullLogger<Ext>.Instance);

    private static BuiltinArg MakeArg<T>(T val)
        => new(_ => JsonSerializer.Serialize(val, JsonSerializerOptions.Default), JsonSerializerOptions.Default);

    public BuiltinsCompositionTests(ITestOutputHelper output) : base(output)
    {
        var cache = new ImportsCache();
        _imports = new CompositeImportsHandler(_default, [_ext1], cache, false);
    }

    [Fact]
    public void Do()
    {
        var result = _imports.Func(new() { FunctionName = "ext.do" }, MakeArg("t"));
        var ext = new Ext(NullLogger<Ext>.Instance);
        Assert.Equal(ext.Do("t"), result);
    }

    [Fact]
    public void DoMore()
    {
        var input = new DoMoreInput("s", 1);
        var result = _imports.Func(new() { FunctionName = "ext.do_more" }, MakeArg(input));
        Assert.Equal(Ext.DoMore(input), result);
    }

    [Fact]
    public void DoMoreJsonOpts()
    {
        var input = new DoMoreInput("s", 1);

        var result = _imports.Func(
            new() { FunctionName = "ext.do_more_json_opts", JsonSerializerOptions = JsonSerializerOptions.Default },
            MakeArg(input)
            ) as bool?;

        Assert.True(result);
    }

    [Fact]
    public void DoNothing()
    {
        _imports.Func(new() { FunctionName = "ext.do_nothing" });
    }

    [Fact]
    public void ValueTaskBuiltinNotSupported()
    {
        var cache = new ImportsCache();
        Assert.Throws<NotSupportedException>(() => cache.Populate([new ValueTaskExt()], false));
    }

    [Fact]
    public void ValueTaskOfTBuiltinNotSupported()
    {
        var cache = new ImportsCache();
        Assert.Throws<NotSupportedException>(() => cache.Populate([new ValueTaskOfTExt()], false));
    }

    [Fact]
    public void GenericBuiltinNotSupported()
    {
        var cache = new ImportsCache();
        Assert.Throws<NotSupportedException>(() => cache.Populate([new GenericMethodExt()], false));
    }

    [Fact]
    public void TooManyArgumentsNotSupported()
    {
        var cache = new ImportsCache();
        Assert.Throws<NotSupportedException>(() => cache.Populate([new TooManyArgsExt()], false));
    }

    [Fact]
    public void InvalidFifthArgumentNotSupported()
    {
        var cache = new ImportsCache();
        Assert.Throws<NotSupportedException>(() => cache.Populate([new InvalidFifthArgExt()], false));
    }

    [Fact]
    public void FourArgumentsWithContextSupported()
    {
        var cache = new ImportsCache();
        var imports = new IOpaCustomBuiltins[] { new FourArgContextExt() };
        IOpaImportsAbi handler = new CompositeImportsHandler(_default, imports, cache, false);

        var result = handler.Func(
            new() { FunctionName = "ext.four_arg_context" },
            MakeArg("a1"),
            MakeArg("a2"),
            MakeArg("a3"),
            MakeArg("a4")
            );

        Assert.Equal("a1 a2 a3 a4", result);
    }

    [Fact]
    public void OneArgumentWithContextSupported()
    {
        var cache = new ImportsCache();
        var imports = new IOpaCustomBuiltins[] { new OneArgContextExt() };
        IOpaImportsAbi handler = new CompositeImportsHandler(_default, imports, cache, false);

        var result = handler.Func(new() { FunctionName = "ext.one_arg_context" }, MakeArg("a1"));

        Assert.Equal("a1", result);
    }

    [Fact]
    public void DoubleSpecialTrailingArgumentNotSupported()
    {
        var cache = new ImportsCache();
        Assert.Throws<NotSupportedException>(() => cache.Populate([new DoubleSpecialArgExt()], false));
    }
}

file record DoMoreInput(string InA, int InB);

file record DoMoreOutput(string A, int B);

file class Ext(ILogger<Ext> logger) : IOpaCustomBuiltins
{
    [OpaCustomBuiltin("ext.do")]
    public string Do(string message)
    {
        logger.LogDebug("{Func} {Message}", nameof(Do), message);
        return $"Hi {message}";
    }

    [OpaCustomBuiltin("ext.do_more")]
    public static DoMoreOutput DoMore(DoMoreInput n) => new(n.InA, n.InB);

    [OpaCustomBuiltin("ext.do_more_json_opts")]
    public static bool DoMore(DoMoreInput n, JsonSerializerOptions? opts) => opts != null;

    [OpaCustomBuiltin("ext.do_nothing")]
    public void DoNothing()
    {
        logger.LogDebug("Nothing");
    }

    public void Reset()
    {
        logger.LogDebug("{Func}", nameof(Reset));
    }
}

file class ValueTaskExt : IOpaCustomBuiltins
{
    [OpaCustomBuiltin("ext.value_task")]
    public static ValueTask ValueTaskBuiltin(string arg1) => ValueTask.CompletedTask;

    public void Reset()
    {
    }
}

file class ValueTaskOfTExt : IOpaCustomBuiltins
{
    [OpaCustomBuiltin("ext.value_task_of_t")]
    public static ValueTask<string> ValueTaskOfTBuiltin(string arg1) => ValueTask.FromResult(arg1);

    public void Reset()
    {
    }
}

file class GenericMethodExt : IOpaCustomBuiltins
{
    [OpaCustomBuiltin("ext.generic")]
    public static T GenericBuiltin<T>(T arg1) => arg1;

    public void Reset()
    {
    }
}

file class TooManyArgsExt : IOpaCustomBuiltins
{
    [OpaCustomBuiltin("ext.too_many")]
    public static string TooManyArgsBuiltin(string a1, string a2, string a3, string a4, string a5, string a6)
        => $"{a1}{a2}{a3}{a4}{a5}{a6}";

    public void Reset()
    {
    }
}

file class InvalidFifthArgExt : IOpaCustomBuiltins
{
    [OpaCustomBuiltin("ext.invalid_fifth")]
    public static string InvalidFifthArgBuiltin(string a1, string a2, string a3, string a4, string a5)
        => $"{a1}{a2}{a3}{a4}{a5}";

    public void Reset()
    {
    }
}

file class FourArgContextExt : IOpaCustomBuiltins
{
    [OpaCustomBuiltin("ext.four_arg_context")]
    public static string FourArgContextBuiltin(string a1, string a2, string a3, string a4, IOpaCustomBuiltinsContext context)
        => context == null ? throw new InvalidOperationException() : $"{a1} {a2} {a3} {a4}";

    public void Reset()
    {
    }
}

file class OneArgContextExt : IOpaCustomBuiltins
{
    [OpaCustomBuiltin("ext.one_arg_context")]
    public static string OneArgContextBuiltin(string a1, IOpaCustomBuiltinsContext context)
        => context == null ? throw new InvalidOperationException() : a1;

    public void Reset()
    {
    }
}

file class DoubleSpecialArgExt : IOpaCustomBuiltins
{
    [OpaCustomBuiltin("ext.double_special")]
    public static string DoubleSpecialBuiltin(string a1, JsonSerializerOptions opts, IOpaCustomBuiltinsContext context)
        => a1;

    public void Reset()
    {
    }
}