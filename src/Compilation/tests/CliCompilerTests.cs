using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;
using OpaDotNet.InternalTesting;

using Xunit.Abstractions;

namespace OpaDotNet.Compilation.Tests;

[UsedImplicitly]
[Trait(Utils.CompilerTrait, Utils.CliCompilerTrait)]
public class CliCompilerTests : CompilerTests<RegoCliCompiler>
{
    public CliCompilerTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override string BaseOutputPath => "cli";

    protected override RegoCliCompiler CreateCompiler(ILoggerFactory? loggerFactory = null)
    {
        return new RegoCliCompiler(loggerFactory?.CreateLogger<RegoCliCompiler>(), null);
    }

    [Fact]
    public async Task OpaCliNotFound()
    {
        var opts = new RegoCliCompilerOptions
        {
            OpaToolPath = "./somewhere",
            ExtraArguments = "--debug",
        };

        var compiler = new RegoCliCompiler(NullLogger<RegoCliCompiler>.Instance, opts);

        _ = await Assert.ThrowsAsync<RegoCompilationException>(
            () => compiler.CompileFileAsync("fail.rego", new())
            );
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
            PreserveBuildArtifacts = true,
        };

        var compiler = new RegoCliCompiler(
            LoggerFactory.CreateLogger<RegoCliCompiler>(), opts
            );

        var policy = await compiler.CompileBundleAsync(
            Path.Combine("TestData", "capabilities"),
            new()
            {
                CapabilitiesVersion = DefaultCaps,
                OutputPath = di.FullName,
                Entrypoints = ["capabilities/f"],
                CapabilitiesFilePath = Path.Combine("TestData", "capabilities", "capabilities.json"),
            }
            );

        Assert.IsType<FileStream>(policy);

        await policy.DisposeAsync();

        Assert.True(Directory.Exists(di.FullName));

        var files = Directory.GetFiles(di.FullName);

        Assert.Equal(2, files.Length);
        Assert.Contains(files, p => p.EndsWith("tar.gz"));
        Assert.Contains(files, p => p.EndsWith("json"));
    }
}