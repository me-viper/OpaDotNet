using OpaDotNet.Wasm.Builtins;
using OpaDotNet.Wasm.Tests.Common;

using Xunit.Abstractions;

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
        var cache = new ImportsCache(JsonSerializerOptions.Default);
        _imports = new CompositeImportsHandler(_default, [_ext1], cache);
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
        var result = _imports.Func(new() { FunctionName = "ext.do_more_json_opts" }, MakeArg(input)) as bool?;
        Assert.True(result);
    }

    [Fact]
    public void DoNothing()
    {
        _imports.Func(new() { FunctionName = "ext.do_nothing" });
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