﻿using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Features;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class BuiltinsCompositionTests : OpaTestBase
{
    private readonly IOpaImportsAbi _imports;

    private readonly IOpaImportsAbi _default = new DefaultOpaImportsAbi();

    private readonly IOpaBuiltinsExtension _ext1 = new Ext(NullLogger<Ext>.Instance);

    private static BuiltinArg MakeArg<T>(T val)
        => new(_ => JsonSerializer.Serialize(val, JsonSerializerOptions.Default), JsonSerializerOptions.Default);

    public BuiltinsCompositionTests(ITestOutputHelper output) : base(output)
    {
        _imports = new OpaCompositeBuiltins(_default, [_ext1], JsonSerializerOptions.Default);
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
    public void DNothing()
    {
        _imports.Func(new() { FunctionName = "ext.do_nothing" });
    }
}

file record DoMoreInput(string InA, int InB);

file record DoMoreOutput(string A, int B);

file class Ext(ILogger<Ext> logger) : IOpaBuiltinsExtension
{
    [OpaImport("ext.do")]
    public string Do(string message)
    {
        logger.LogDebug("{Func} {Message}", nameof(Do), message);
        return $"Hi {message}";
    }

    [OpaImport("ext.do_more")]
    public static DoMoreOutput DoMore(DoMoreInput n) => new(n.InA, n.InB);

    [OpaImport("ext.do_more_json_opts")]
    public static bool DoMore(DoMoreInput n, JsonSerializerOptions? opts) => opts != null;

    [OpaImport("ext.do_nothing")]
    public void DoNothing()
    {
        logger.LogDebug("Nothing");
    }

    public void Reset()
    {
        logger.LogDebug("{Func}", nameof(Reset));
    }
}