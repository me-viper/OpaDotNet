using System.Text.Json;

using BenchmarkDotNet.Attributes;

using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Features;

namespace OpaDotNet.Benchmarks;

[Config(typeof(Config))]
public class Imports
{
    private static BuiltinArg MakeArg<T>(T val)
        => new(_ => JsonSerializer.Serialize(val, JsonSerializerOptions.Default), JsonSerializerOptions.Default);

    private readonly IOpaImportsAbi _imports;

    private readonly IOpaImportsAbi _default = new Def();

    private readonly Ext _ext1 = new();

    private BuiltinArg Arg { get; } = MakeArg("test");

    public Imports()
    {
        _imports = new OpaImportsHandler(_default, [_ext1], JsonSerializerOptions.Default);
    }

    [Benchmark(Baseline = true)]
    public object Default()
    {
        var result = _ext1.Do("test");

        if (result == null)
            throw new InvalidOperationException();

        return result;
    }

    [Benchmark]
    public object Import()
    {
        var result = _default.Func(new() { FunctionName = "ext.do" }, Arg);

        if (result == null)
            throw new InvalidOperationException();

        return result;
    }

    [Benchmark]
    public object Extension()
    {
        var result = _imports.Func(new() { FunctionName = "ext.do" }, Arg);

        if (result == null)
            throw new InvalidOperationException();

        return result;
    }
}

internal class Ext : IOpaImportExtension
{
    [OpaImport("ext.do")]
    public string Do(string message)
    {
        return $"Hi {message}";
    }

    public void Reset()
    {
    }
}

internal class Def : IOpaImportsAbi
{
    public void Print(IEnumerable<string> args)
    {
        throw new NotImplementedException();
    }

    public object? Func(BuiltinContext context)
        => throw new NotImplementedException();

    public object? Func(BuiltinContext context, BuiltinArg arg1)
        => $"Hi {arg1.As<string>()}";

    public object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2)
        => throw new NotImplementedException();

    public object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3)
        => throw new NotImplementedException();

    public object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3, BuiltinArg arg4)
        => throw new NotImplementedException();

    public void Reset()
    {
    }
}