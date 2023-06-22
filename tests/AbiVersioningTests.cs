﻿using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class AbiVersioningTests : IAsyncLifetime
{
    private OpaEvaluatorFactory _factory = default!;

    private readonly ITestOutputHelper _output;

    private readonly ILoggerFactory _loggerFactory;

    private const string AbiVersion = "1.2";

    private string BasePath { get; } = Path.Combine("TestData", "Opa", "basics");

    public AbiVersioningTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
    }

    public Task InitializeAsync()
    {
        _factory = new OpaEvaluatorFactory(loggerFactory: _loggerFactory);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData("1.0", "1.0")]
    [InlineData("1.1", "1.0")]
    [InlineData("1.2", "1.2")]
    [InlineData("10.0", "1.3")]
    [InlineData(null, "1.3")]
    public void MaxAbiVersion(string? ver, string expectedVersion)
    {
        var abiVer = string.IsNullOrWhiteSpace(ver) ? null : Version.Parse(ver);
        var expectedVer = Version.Parse(expectedVersion);

        var engine = _factory.CreateFromWasm(
            File.OpenRead(Path.Combine(BasePath, "simple-1.3.wasm")),
            options: new() { MaxAbiVersion = abiVer }
            );

        engine.SetDataFromRawJson("{ \"world\": \"world\" }");
        
        Assert.Equal(expectedVer, engine.AbiVersion);
    }

    [Fact]
    public void PolicyAbiVersion()
    {
        var engine = _factory.CreateFromWasm(
            File.OpenRead(Path.Combine(BasePath, "simple-1.2.wasm"))
            );
        
        engine.SetDataFromRawJson("{ \"world\": \"world\" }");

        Assert.Equal(new Version(1, 2), ((WasmOpaEvaluator)engine).PolicyAbiVersion);
    }
}