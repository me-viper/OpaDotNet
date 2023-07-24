using Microsoft.Extensions.Options;

using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class RegoCliCompilerTests
{
    private readonly ILoggerFactory _loggerFactory;

    private readonly DirectoryInfo _outputPath;

    public RegoCliCompilerTests(ITestOutputHelper output)
    {
        _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });

        _outputPath = new DirectoryInfo("./build");

        if (!_outputPath.Exists)
            _outputPath.Create();
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
        var opts = new RegoCliCompilerOptions
        {
            OutputPath = _outputPath.FullName,
        };

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
            OutputPath = _outputPath.FullName,
        };

        var compiler = new RegoCliCompiler(
            new OptionsWrapper<RegoCliCompilerOptions>(opts),
            _loggerFactory.CreateLogger<RegoCliCompiler>()
            );

        await using var policy = await compiler.CompileBundle(
            Path.Combine("TestData", "capabilities"),
            new[] { "capabilities/f" },
            Path.Combine("TestData", "capabilities", "capabilities.json")
            );

        Assert.NotNull(policy);
    }

    [Fact]
    public async Task Compile()
    {
        var compiler = new RegoCliCompiler(
            new OptionsWrapper<RegoCliCompilerOptions>(new() { ExtraArguments = "--debug" }),
            _loggerFactory.CreateLogger<RegoCliCompiler>()
            );

        await using var policy = await compiler.CompileBundle(
            Path.Combine("TestData", "compile-bundle", "example"),
            new[] { "test1/hello", "test2/hello" }
            );

        var evaluator = OpaEvaluatorFactory.CreateFromBundle(policy);

        var input1 = new { message = "world" };
        var test1Result = evaluator.EvaluatePredicate(input1, "test1/hello");

        Assert.True(test1Result.Result);

        var input2 = new { message = "world1" };
        var test2Result = evaluator.EvaluatePredicate(input2, "test2/hello");

        Assert.True(test2Result.Result);
    }

    [Fact]
    public Task FileBundle()
    {
        var path = Path.Combine("TestData", "compile-bundle", "bundle.tar.gz");

        var evaluator = OpaEvaluatorFactory.CreateFromBundle(File.OpenRead(path));

        var input1 = new { message = "world" };
        var test1Result = evaluator.EvaluatePredicate(input1, "test1/hello");

        Assert.True(test1Result.Result);

        var input2 = new { message = "world1" };
        var test2Result = evaluator.EvaluatePredicate(input2, "test2/hello");

        Assert.True(test2Result.Result);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task PreserveBuildArtifacts()
    {
        var di = new DirectoryInfo("buildArtifacts");

        if (di.Exists)
            di.Delete(true);

        di.Create();

        var opts = new RegoCliCompilerOptions
        {
            CapabilitiesVersion = "v0.53.1",
            PreserveBuildArtifacts = true,
            OutputPath = di.FullName,
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

        Assert.IsType<FileStream>(policy);

        await policy.DisposeAsync();

        Assert.True(Directory.Exists(di.FullName));

        var files = Directory.GetFiles(di.FullName);
        Assert.Collection(
            files,
            p => Assert.EndsWith("tar.gz", p, StringComparison.Ordinal),
            p => Assert.EndsWith("json", p, StringComparison.Ordinal)
            );
    }
}