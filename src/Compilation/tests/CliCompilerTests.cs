using JetBrains.Annotations;

using Microsoft.Extensions.Logging;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;

using Xunit.Abstractions;

namespace OpaDotNet.Compilation.Tests;

[UsedImplicitly]
[Trait("NeedsCli", "true")]
[Trait("Category", "Cli")]
public class CliCompilerTests : CompilerTests<RegoCliCompiler>
{
    public CliCompilerTests(ITestOutputHelper output) : base(output)
    {
    }

    protected override string BaseOutputPath => "cli";

    protected override RegoCliCompiler CreateCompiler(ILoggerFactory? loggerFactory = null)
    {
        return new RegoCliCompiler(null, loggerFactory?.CreateLogger<RegoCliCompiler>());
    }

    [Fact]
    public async Task OpaCliNotFound()
    {
        var opts = new RegoCliCompilerOptions
        {
            OpaToolPath = "./somewhere",
            ExtraArguments = "--debug",
        };

        var compiler = new RegoCliCompiler(opts);

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
            opts,
            LoggerFactory.CreateLogger<RegoCliCompiler>()
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