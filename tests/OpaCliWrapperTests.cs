using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm.Compilation;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

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
            () => OpaCliWrapper.Create("./opa", logger: _loggerFactory.CreateLogger<OpaCliWrapper>())
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