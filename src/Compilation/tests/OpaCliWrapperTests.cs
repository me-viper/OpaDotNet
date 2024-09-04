using Microsoft.Extensions.Logging;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;
using OpaDotNet.Compilation.Tests.Common;
using OpaDotNet.InternalTesting;

using Xunit.Abstractions;

namespace OpaDotNet.Compilation.Tests;

[Trait("NeedsCli", "true")]
public class OpaCliWrapperTests
{
    private readonly ITestOutputHelper _output;

    private readonly ILoggerFactory _loggerFactory;

    public OpaCliWrapperTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
    }

    [Fact]
    public async Task ToolMissing()
    {
        _ = await Assert.ThrowsAsync<RegoCompilationException>(
            () => OpaCliWrapper.Create("./non-opa", logger: _loggerFactory.CreateLogger<OpaCliWrapper>())
            );
    }

    [Fact]
    public async Task Version()
    {
        var result = await OpaCliWrapper.Create(logger: _loggerFactory.CreateLogger<OpaCliWrapper>());

        _output.WriteLine(result.VersionInfo.ToString());

        Assert.NotNull(result.VersionInfo.Version);

        var ver = System.Version.Parse(result.VersionInfo.Version);
        _output.WriteLine($"Resolved version: {ver}");

        Assert.NotNull(result.VersionInfo.Commit);
        Assert.NotNull(result.VersionInfo.Timestamp);
        Assert.NotNull(result.VersionInfo.GoVersion);
        Assert.NotNull(result.VersionInfo.Platform);
        Assert.NotNull(result.VersionInfo.WebAssembly);
    }
}