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

        _ = await Assert.ThrowsAsync<RegoCompilationException>(
            () => compiler.CompileFile("fail.rego")
            );
    }

    [Fact]
    public async Task FailCapabilities()
    {
        var opts = new RegoCliCompilerOptions
        {
            OutputPath = _outputPath.FullName,
        };

        var compiler = new RegoCliCompiler(new OptionsWrapper<RegoCliCompilerOptions>(opts));

        _ = await Assert.ThrowsAsync<RegoCompilationException>(
            () => compiler.CompileBundle(
                Path.Combine("TestData", "capabilities"),
                new[] { "capabilities/f" },
                Path.Combine("TestData", "capabilities", "capabilities.json")
                )
            );
    }

    [Fact]
    public async Task FailCompilation()
    {
        var compiler = new RegoCliCompiler();
        var ex = await Assert.ThrowsAsync<RegoCompilationException>(() => compiler.CompileSource("bad rego", new[] { "ep" }));

        Assert.Contains("rego_parse_error: package expected", ex.Message);
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

        Assert.Equal(2, files.Length);
        Assert.Contains(files, p => p.EndsWith("tar.gz"));
        Assert.Contains(files, p => p.EndsWith("json"));
    }

    [Fact]
    public Task StreamFileBundle()
    {
        var di = new DirectoryInfo("cache");

        if (di.Exists)
            di.Delete(true);

        di.Create();

        var path = Path.Combine("TestData", "compile-bundle", "bundle.tar.gz");

        var opts = new WasmPolicyEngineOptions
        {
            CachePath = di.FullName,
        };

        var factory = new OpaBundleEvaluatorFactory(File.OpenRead(path), opts);

        var evaluator1 = factory.Create();
        var input1 = new { message = "world" };
        var test1Result = evaluator1.EvaluatePredicate(input1, "test1/hello");

        Assert.True(test1Result.Result);

        var evaluator2 = factory.Create();
        var input2 = new { message = "world" };
        var test2Result = evaluator2.EvaluatePredicate(input2, "test1/hello");

        Assert.True(test2Result.Result);

        var cache = di.GetDirectories();
        Assert.Single(cache);

        var files = cache[0].GetFiles();

        Assert.Contains(files, p => string.Equals(p.Name, "policy.wasm", StringComparison.Ordinal));
        Assert.Contains(files, p => string.Equals(p.Name, "data.json", StringComparison.Ordinal));

        factory.Dispose();

        Assert.False(Directory.Exists(cache[0].FullName));

        return Task.CompletedTask;
    }

    [Fact]
    public Task StreamFileWasm()
    {
        var di = new DirectoryInfo("cache");

        if (di.Exists)
            di.Delete(true);

        di.Create();

        var policyPath = Path.Combine("TestData", "compile-bundle", "policy.wasm");
        var dataPath = Path.Combine("TestData", "compile-bundle", "data.json");

        var opts = new WasmPolicyEngineOptions
        {
            CachePath = di.FullName,
        };

        var factory = new OpaWasmEvaluatorFactory(File.OpenRead(policyPath), opts);

        var evaluator1 = factory.Create();
        evaluator1.SetDataFromStream(File.OpenRead(dataPath));
        var input1 = new { message = "world" };
        var test1Result = evaluator1.EvaluatePredicate(input1, "test1/hello");

        Assert.True(test1Result.Result);

        var evaluator2 = factory.Create();
        evaluator2.SetDataFromStream(File.OpenRead(dataPath));
        var input2 = new { message = "world" };
        var test2Result = evaluator2.EvaluatePredicate(input2, "test1/hello");

        Assert.True(test2Result.Result);

        var cache = di.GetDirectories();
        Assert.Single(cache);

        var files = cache[0].GetFiles();

        Assert.Contains(files, p => string.Equals(p.Name, "policy.wasm", StringComparison.Ordinal));

        factory.Dispose();

        Assert.False(Directory.Exists(cache[0].FullName));

        return Task.CompletedTask;
    }
}