using Microsoft.Extensions.Options;

using OpaDotNet.Extensions.AspNetCore.Telemetry;

namespace OpaDotNet.Extensions.AspNetCore;

public abstract class PathPolicySource : OpaPolicySource
{
    protected IDisposable? PolicyWatcher { get; init; }

    private readonly PeriodicTimer? _changesMonitor;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    protected bool NeedsRecompilation { get; set; }

    protected bool MonitoringEnabled => Options.MonitoringInterval > TimeSpan.Zero;

    protected PathPolicySource(
        IOptionsMonitor<OpaAuthorizationOptions> options,
        IOpaBundleEvaluatorFactoryBuilder factoryBuilder,
        ILoggerFactory loggerFactory) : base(options, factoryBuilder, loggerFactory)
    {
        if (string.IsNullOrWhiteSpace(Options.PolicyBundlePath))
        {
            throw new InvalidOperationException(
                $"{GetType()} requires OpaAuthorizationOptions.PolicyBundlePath specified"
                );
        }

        if (MonitoringEnabled)
            _changesMonitor = new(Options.MonitoringInterval);
    }

    private async Task TrackPolicyChanged(CancellationToken cancellationToken)
    {
        if (!MonitoringEnabled || _changesMonitor == null)
            return;

        Logger.PolicySourceWatchStarted(Options.PolicyBundlePath!);

        while (await _changesMonitor.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                if (!NeedsRecompilation)
                    continue;

                if (cancellationToken.IsCancellationRequested)
                    break;

                NeedsRecompilation = false;

                Logger.BundleCompilationHasChanges();
                await CompileBundle(true, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                NeedsRecompilation = true;
            }
        }

        Logger.PolicySourceWatchStopped();
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing)
            {
                _changesMonitor?.Dispose();
                PolicyWatcher?.Dispose();
                _cancellationTokenSource.Dispose();
            }
        }
        finally
        {
            base.Dispose(disposing);
        }
    }

    /// <inheritdoc/>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken).ConfigureAwait(false);

        if (MonitoringEnabled)
        {
            _ = Task.Run(() => TrackPolicyChanged(_cancellationTokenSource.Token), cancellationToken);
        }
    }

    /// <inheritdoc/>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);

#if NET8_0_OR_GREATER
        await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
#else
        _cancellationTokenSource.Cancel();
#endif

        Logger.ServiceStopped();
    }
}