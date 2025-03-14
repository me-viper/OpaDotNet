﻿using OpaDotNet.InternalTesting;
using OpaDotNet.Wasm.Features;
using OpaDotNet.Wasm.Internal;

namespace OpaDotNet.Wasm.Tests;

public class ExtensibilityTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;

    private readonly ILoggerFactory _loggerFactory;

    private string BasePath { get; } = Path.Combine("TestData", "basics");

    public ExtensibilityTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerFactory = new LoggerFactory([new XunitLoggerProvider(output)]);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _loggerFactory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public void V12()
    {
        const string data = "{ \"world\": \"world\" }";
        var input = new { message = "world" };

        using var wasm = File.OpenRead(Path.Combine(BasePath, "simple-1.2.wasm"));
        using var engine = OpaWasmEvaluatorFactory.Create(wasm);

        Assert.Equal(new Version(1, 2), engine.AbiVersion);

        engine.SetDataFromRawJson(data);

        var result = engine.EvaluatePredicate(input);

        Assert.True(result.Result);

        var gotExtension = engine.TryGetFeature<IUpdateDataFeature>(out _);

        Assert.False(gotExtension);
    }

    [Fact]
    public void V13()
    {
        const string data = "{ \"world\": \"world\" }";
        var input = new { message = "world" };

        using var wasm = File.OpenRead(Path.Combine(BasePath, "simple-1.3.wasm"));
        using var engine = OpaWasmEvaluatorFactory.Create(wasm);

        Assert.Equal(new Version(1, 3), engine.AbiVersion);

        engine.SetDataFromRawJson(data);

        var result = engine.EvaluatePredicate(input);

        Assert.True(result.Result);

        var gotExtension = engine.TryGetFeature<IUpdateDataFeature>(out _);

        Assert.True(gotExtension);
    }

    [Fact]
    public void UpdateData()
    {
        const string data = "{\"world\":\"world\"}";
        var initialInput = new { message = "world" };

        using var wasm = File.OpenRead(Path.Combine(BasePath, "simple-1.3.wasm"));
        using var engine = (OpaWasmEvaluator)OpaWasmEvaluatorFactory.Create(wasm);

        Assert.Equal(new Version(1, 3), engine.AbiVersion);

        engine.SetDataFromRawJson(data);
        var initialData = engine.DumpData();
        Assert.Equal(data, initialData);

        var result1 = engine.EvaluatePredicate(initialInput);
        Assert.True(result1.Result);

        var gotExtension = engine.TryGetFeature<IUpdateDataFeature>(out var ext);
        Assert.True(gotExtension);
        Assert.NotNull(ext);

        ext.UpdateDataPath("\"new\"", ["world"]);
        var updatedData = engine.DumpData();
        Assert.Equal("{\"world\":\"new\"}", updatedData);

        var changedInput = new { message = "new" };
        var result2 = engine.EvaluatePredicate(changedInput);
        Assert.True(result2.Result);
    }

    [Fact]
    public void AddData()
    {
        const string data = "{\"world\":\"world\"}";
        var initialInput = new { message = "world" };

        using var wasm = File.OpenRead(Path.Combine(BasePath, "simple-1.3.wasm"));
        using var engine = (OpaWasmEvaluator)OpaWasmEvaluatorFactory.Create(wasm);

        Assert.Equal(new Version(1, 3), engine.AbiVersion);

        engine.SetDataFromRawJson(data);
        var initialData = engine.DumpData();
        Assert.Equal(data, initialData);

        var result1 = engine.EvaluatePredicate(initialInput);
        Assert.True(result1.Result);

        var gotExtension = engine.TryGetFeature<IUpdateDataFeature>(out var ext);
        Assert.True(gotExtension);
        Assert.NotNull(ext);

        ext.UpdateDataPath("{\"path\":\"world\"}", ["new"]);
        var updatedData = engine.DumpData();
        Assert.Equal("{\"new\":{\"path\":\"world\"},\"world\":\"world\"}", updatedData);

        var result2 = engine.EvaluatePredicate(initialInput);
        Assert.True(result2.Result);
    }

    [Fact]
    public void RemoveData()
    {
        const string data = "{\"new\":{\"path\":\"world\"},\"world\":\"world\"}";
        var initialInput = new { message = "world" };

        using var wasm = File.OpenRead(Path.Combine(BasePath, "simple-1.3.wasm"));
        using var engine = (OpaWasmEvaluator)OpaWasmEvaluatorFactory.Create(wasm);

        Assert.Equal(new Version(1, 3), engine.AbiVersion);

        engine.SetDataFromRawJson(data);
        var initialData = engine.DumpData();
        Assert.Equal(data, initialData);

        var result1 = engine.EvaluatePredicate(initialInput);
        Assert.True(result1.Result);

        var gotExtension = engine.TryGetFeature<IUpdateDataFeature>(out var ext);
        Assert.True(gotExtension);
        Assert.NotNull(ext);

        ext.RemoveDataPath(["new"]);
        var updatedData = engine.DumpData();
        Assert.Equal("{\"world\":\"world\"}", updatedData);

        var result2 = engine.EvaluatePredicate(initialInput);
        Assert.True(result2.Result);
    }
}