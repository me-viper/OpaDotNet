using Microsoft.Extensions.Logging;

using OpaDotNet.InternalTesting;

namespace OpaDotNet.Extensions.AspNetCore.Tests;

[Collection("Sequential")]
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
            LoggerFactory.Dispose();
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

    //[InlineData(false)]
    public async Task WatchChanges(bool usePolingWatcher)
    {
        await WritePolicy(Policy(0));

        if (usePolingWatcher)
            Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
        else
            Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "0");

        using var source = CreatePolicySource(false, p => p.MonitoringInterval = TimeSpan.FromSeconds(3));

        await source.StartAsync(CancellationToken.None);

        for (var i = 0; i < 3; i++)
        {
            var tcs = new TaskCompletionSource();

            using var eval = source.CreateEvaluator();
            var result = eval.EvaluatePredicate(new UserPolicyInput($"u{i}"));

            _output.WriteLine($"Checking: u{i}");
            Assert.True(result.Result);

            // Subscribe before writing so a fast recompilation can't fire before we're listening.
            var token = source.OnPolicyUpdated();
            using var _ = token.RegisterChangeCallback(_ => tcs.TrySetResult(), null);

            await WritePolicy(Policy(i + 1));

            // Polling watcher (~4s) plus MonitoringInterval (3s) can exceed 10s on slow CI runners.
            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
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