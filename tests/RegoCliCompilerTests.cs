using Microsoft.Extensions.Options;

using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm.Compilation;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class RegoCliCompilerTests
{
    private readonly ILoggerFactory _loggerFactory;

    public RegoCliCompilerTests(ITestOutputHelper output)
    {
        _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
    }

    [Fact]
    public async Task OpaCliNotFound()
    {
        var opts = new RegoCliCompilerOptions
        {
            OpaToolPath = "./somewhere",
        };

        var compiler = new RegoCliCompiler(new OptionsWrapper<RegoCliCompilerOptions>(opts));

        var ex = await Assert.ThrowsAsync<RegoCompilationException>(
            () => compiler.CompileFile("fail.rego")
            );

        Assert.Equal("fail.rego", ex.SourceFile);
    }

    [Fact]
    public async Task FailCapabilities()
    {
        var opts = new RegoCliCompilerOptions();

        var compiler = new RegoCliCompiler(new OptionsWrapper<RegoCliCompilerOptions>(opts));

        var ex = await Assert.ThrowsAsync<RegoCompilationException>(
            () => compiler.CompileBundle(
                Path.Combine("TestData", "capabilities"),
                new[] { "capabilities/f" },
                Path.Combine("TestData", "capabilities", "capabilities.json")
                )
            );

        var f = new FileInfo(Path.Combine("TestData", "capabilities"));
        Assert.Equal(f.FullName, ex.SourceFile);
    }

    [Fact]
    public async Task MergeCapabilities()
    {
        var opts = new RegoCliCompilerOptions
        {
            CapabilitiesVersion = "v0.53.1",
        };

        var compiler = new RegoCliCompiler(
            new OptionsWrapper<RegoCliCompilerOptions>(opts),
            _loggerFactory.CreateLogger<RegoCliCompiler>()
            );

        var policy = await compiler.CompileBundle(
            Path.Combine("TestData", "capabilities"),
            new[] { "capabilities/f" },
            Path.Combine("TestData", "capabilities", "capabilities.json")
            );

        Assert.NotNull(policy);
    }
}