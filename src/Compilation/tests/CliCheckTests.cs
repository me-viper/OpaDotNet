using System.Text.Json;

using Microsoft.Extensions.Logging;

using OpaDotNet.Compilation.Cli;
using OpaDotNet.InternalTesting;

namespace OpaDotNet.Compilation.Tests;

public class CliCheckTests(ITestOutputHelper output)
{
    private readonly ILoggerFactory _loggerFactory = new LoggerFactory([new XunitLoggerProvider(output)]);

    [Fact]
    public async Task CheckBundle()
    {
        var opts = new RegoCliCompilerOptions
        {
            PreserveBuildArtifacts = true,
        };

        var compiler = new RegoCliCompiler(_loggerFactory.CreateLogger<RegoCliCompiler>(), opts);

        var result = await compiler.CheckBundleAsync(
            Path.Combine("TestData", "check"),
            new() { IsBundle = true, Strict = true, Format = CheckOutputFormat.Json },
            TestContext.Current.CancellationToken
            );

        output.WriteLine(result.Output ?? string.Empty);

        Assert.False(result.Success);
        Assert.Equal(1, result.ExitCode);
        Assert.NotNull(result.Output);

        var errJson = JsonDocument.Parse(result.Output);
        errJson.RootElement.TryGetProperty("errors", out var err);

        Assert.True(err.GetArrayLength() > 0);
    }

    [Fact]
    public async Task CheckFile()
    {
        var opts = new RegoCliCompilerOptions
        {
            PreserveBuildArtifacts = true,
        };

        var compiler = new RegoCliCompiler(_loggerFactory.CreateLogger<RegoCliCompiler>(), opts);

        var result = await compiler.CheckFileAsync(
            Path.Combine("TestData", "check", "bad.rego"),
            new() { Strict = true, Format = CheckOutputFormat.Json },
            TestContext.Current.CancellationToken
            );

        output.WriteLine(result.Output ?? string.Empty);

        Assert.False(result.Success);
        Assert.Equal(1, result.ExitCode);
        Assert.NotNull(result.Output);

        var errJson = JsonDocument.Parse(result.Output);
        errJson.RootElement.TryGetProperty("errors", out var err);

        Assert.True(err.GetArrayLength() > 0);
    }

    [Fact]
    public async Task CheckSourceError()
    {
        var opts = new RegoCliCompilerOptions
        {
            PreserveBuildArtifacts = true,
        };

        var compiler = new RegoCliCompiler(_loggerFactory.CreateLogger<RegoCliCompiler>(), opts);
        var src = """
            package ex

            import future.keywords.if

            default test := true

            # METADATA
            # entrypoint: true
            test_entrypoint if {
                a := 1
                true
            }
            """;

        var result = await compiler.CheckSourceAsync(
            src,
            new() { Strict = true, Format = CheckOutputFormat.Json },
            TestContext.Current.CancellationToken
            );

        output.WriteLine(result.Output ?? string.Empty);

        Assert.False(result.Success);
        Assert.Equal(1, result.ExitCode);
        Assert.NotNull(result.Output);

        var errJson = JsonDocument.Parse(result.Output);
        errJson.RootElement.TryGetProperty("errors", out var err);

        Assert.True(err.GetArrayLength() > 0);
    }

    [Fact]
    public async Task CheckSourceOk()
    {
        var opts = new RegoCliCompilerOptions();

        var compiler = new RegoCliCompiler(_loggerFactory.CreateLogger<RegoCliCompiler>(), opts);
        var src = """
            package ex

            import future.keywords.if

            default test := true

            # METADATA
            # entrypoint: true
            test_entrypoint if {
                true
            }
            """;

        var result = await compiler.CheckSourceAsync(
            src,
            new() { Strict = true, Format = CheckOutputFormat.Json },
            TestContext.Current.CancellationToken
            );

        output.WriteLine(result.Output ?? string.Empty);

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task MergeCapabilities()
    {
        var opts = new RegoCliCompilerOptions
        {
            PreserveBuildArtifacts = false,
        };

        var compiler = new RegoCliCompiler(_loggerFactory.CreateLogger<RegoCliCompiler>(), opts);

        var result = await compiler.CheckBundleAsync(
            Path.Combine("TestData", "capabilities"),
            new()
            {
                CapabilitiesFilePath = Path.Combine("TestData", "capabilities", "capabilities.json"),
                CapabilitiesVersion = Utils.DefaultCapabilities,
            },
            TestContext.Current.CancellationToken
            );

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task CheckBundleFromBundle()
    {
        var opts = new RegoCliCompilerOptions
        {
            PreserveBuildArtifacts = false,
        };

        var compiler = new RegoCliCompiler(_loggerFactory.CreateLogger<RegoCliCompiler>(), opts);

        var result = await compiler.CheckBundleAsync(
            Path.Combine("TestData", "src.bundle.tar.gz"),
            new()
            {
                CapabilitiesFilePath = Path.Combine("TestData", "capabilities", "capabilities.json"),
                CapabilitiesVersion = Utils.DefaultCapabilities,
            },
            TestContext.Current.CancellationToken
            );

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task CheckBundleStream()
    {
        var opts = new RegoCliCompilerOptions
        {
            PreserveBuildArtifacts = false,
        };

        var compiler = new RegoCliCompiler(_loggerFactory.CreateLogger<RegoCliCompiler>(), opts);
        await using var fs = new FileStream(Path.Combine("TestData", "src.bundle.tar.gz"), FileMode.Open);

        var result = await compiler.CheckBundleAsync(
            fs,
            new()
            {
                CapabilitiesFilePath = Path.Combine("TestData", "capabilities", "capabilities.json"),
                CapabilitiesVersion = Utils.DefaultCapabilities,
            },
            TestContext.Current.CancellationToken
            );

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
    }
}