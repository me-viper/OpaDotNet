using System.Collections.Concurrent;

using Microsoft.Extensions.Options;

using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

namespace Opa.WebApp.Policy;

public sealed class OpaPolicyService : IHostedService, IOpaPolicyService
{
    private IOpaEvaluator _evaluator = default!;

    private readonly IRegoCompiler _compiler;

    private readonly ILogger _logger;

    private readonly ILoggerFactory _loggerFactory;

    private readonly FileSystemWatcher _policyWatcher;

    private readonly IOptions<OpaPolicyServiceOptions> _options;

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly ConcurrentBag<string> _changes = new();

    private readonly PeriodicTimer _changesMonitor;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public OpaPolicyService(
        IRegoCompiler compiler,
        IOptions<OpaPolicyServiceOptions> options,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(compiler);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _compiler = compiler;
        _options = options;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ILogger<OpaPolicyService>>();

        _policyWatcher = new()
        {
            Path = _options.Value.PolicyBundlePath,
            Filters = { "*.rego", "data.json" },
            NotifyFilter = NotifyFilters.LastWrite,
            IncludeSubdirectories = true,
        };

        _policyWatcher.Changed += PolicyChanged;
        _changesMonitor = new(_options.Value.MonitoringInterval);
    }

    private void PolicyChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Changed)
            return;

        var changedFile = new FileInfo(e.FullPath);

        if (!changedFile.Exists)
            return;

        if (_cancellationTokenSource.Token.IsCancellationRequested)
            return;

        _logger.LogDebug("Detected policy change in {File}. Stashing until next recompilation cycle", e.FullPath);
        _changes.Add(changedFile.FullName);
    }

    private async Task CompileBundle(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Compiling");

        try
        {
            await using var policy = await _compiler.CompileBundle(
                _options.Value.PolicyBundlePath,
                cancellationToken: cancellationToken
                );

            _evaluator = OpaEvaluatorFactory.CreateFromBundle(policy, loggerFactory: _loggerFactory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bundle compilation failed");
            throw;
        }

        _logger.LogDebug("Compilation succeeded");
    }

    private async Task TrackPolicyChanged(CancellationToken cancellationToken)
    {
        try
        {
            while (await _changesMonitor.WaitForNextTickAsync(cancellationToken))
            {
                if (_changes.IsEmpty)
                    continue;

                try
                {
                    _logger.LogDebug("Detected changes. Recompiling");
                    await _semaphore.WaitAsync(cancellationToken);

                    await CompileBundle(cancellationToken);
                    _logger.LogDebug("Recompilation succeeded");

                    _changes.Clear();
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process policy changes");
        }
    }

    public async Task<PolicyEvaluationResult<bool>> EvaluatePredicate(
        OpaPolicyInput input,
        string policyName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrEmpty(policyName);

        try
        {
            var token = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, cancellationToken).Token;

            // Opa evaluators are not thread-safe.
            await _semaphore.WaitAsync(token);
            return _evaluator.EvaluatePredicate(input, policyName);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Performing initial bundle compilation");

        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            await CompileBundle(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }

        _logger.LogDebug("Watching policy for changes");
        _policyWatcher.EnableRaisingEvents = true;

        _ = Task.Run(() => TrackPolicyChanged(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

        _logger.LogDebug("Started");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        _policyWatcher.EnableRaisingEvents = false;
        _logger.LogDebug("Stopped");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _changesMonitor.Dispose();
        _policyWatcher.Dispose();
        _cancellationTokenSource.Dispose();
        _evaluator?.Dispose();
    }
}