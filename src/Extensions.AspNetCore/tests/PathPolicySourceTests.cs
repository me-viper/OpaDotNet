using Microsoft.Extensions.Logging;

using OpaDotNet.InternalTesting;

namespace OpaDotNet.Extensions.AspNetCore.Tests;

public abstract class PathPolicySourceTests<T> : IDisposable
    where T : PathPolicySource
{
    private readonly ITestOutputHelper _output;

    protected ILoggerFactory LoggerFactory { get; }

    protected PathPolicySourceTests(ITestOutputHelper output)
    {
        _output = output;
        LoggerFactory = new LoggerFactory([new XunitLoggerProvider(output)]);
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", null);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", null);
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }

    protected abstract T CreatePolicySource(bool forceBundleWriter, Action<OpaAuthorizationOptions>? configure = null);

    protected abstract Task WritePolicy(string policy);

    protected record UserPolicyInput([UsedImplicitly] string User);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Simple(bool forceBundleWriter)
    {
        await WritePolicy(Policy(0));

        using var source = CreatePolicySource(forceBundleWriter);

        await source.StartAsync(CancellationToken.None);

        var eval = source.CreateEvaluator();
        var result = eval.EvaluatePredicate(new UserPolicyInput("u0"));

        _output.WriteLine("Checking: u0");
        Assert.True(result.Result);

        await source.StopAsync(CancellationToken.None);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WatchChanges(bool usePolingWatcher)
    {
        await WritePolicy(Policy(0));

        if (usePolingWatcher)
            Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
        else
            Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "0");

        using var source = CreatePolicySource(false, p => p.MonitoringInterval = TimeSpan.FromSeconds(3));

        await source.StartAsync(CancellationToken.None);
        using var iterate = new AutoResetEvent(false);

        for (var i = 0; i < 3; i++)
        {
            using var eval = source.CreateEvaluator();
            var result = eval.EvaluatePredicate(new UserPolicyInput($"u{i}"));

            _output.WriteLine($"Checking: u{i}");
            Assert.True(result.Result);

            await WritePolicy(Policy(i + 1));

            var token = source.OnPolicyUpdated();
            using var _ = token.RegisterChangeCallback(_ => iterate.Set(), null);

            iterate.WaitOne(TimeSpan.FromSeconds(10));
        }

        await source.StopAsync(CancellationToken.None);
    }

    protected static string Policy(int i)
    {
        return $$"""
            package watch
            import future.keywords.if

            # METADATA
            # entrypoint: true
            user if {
                input.user == "u{{i}}"
            }
            """;
    }
}