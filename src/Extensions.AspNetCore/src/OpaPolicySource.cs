using JetBrains.Annotations;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Extensions.AspNetCore.Telemetry;
using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore;

/// <summary>
/// Compiles policy bundle from source.
/// </summary>
public abstract class OpaPolicySource : IOpaPolicySource
{
    // Has nothing to do with cancellation really but used to notify about recompilation.
    private CancellationTokenSource _changeTokenSource = new();

    private CancellationChangeToken _changeToken;

    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly IOptionsMonitor<OpaAuthorizationOptions> _options;

    protected ILogger Logger { get; }

    /// <summary>
    /// Produces instances of ILogger classes based on the specified providers.
    /// </summary>
    [PublicAPI]
    protected ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// Policy evaluator options.
    /// </summary>
    protected OpaAuthorizationOptions Options => _options.CurrentValue;

    private IOpaImportsAbiFactory ImportsAbiFactory { get; }

    private OpaEvaluatorFactory? _factory;

    /// <inheritdoc />
    public IOpaEvaluator CreateEvaluator()
    {
        if (_factory == null)
            throw new InvalidOperationException("Evaluator factory have not been initialized");

        return _factory.Create();
    }

    protected OpaPolicySource(
        IOptionsMonitor<OpaAuthorizationOptions> options,
        IOpaImportsAbiFactory importsAbiFactory,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _options = options;
        ImportsAbiFactory = importsAbiFactory;
        LoggerFactory = loggerFactory;

        Logger = LoggerFactory.CreateLogger<OpaPolicySource>();
        _changeToken = new(_changeTokenSource.Token);
    }

    /// <inheritdoc />
    public IChangeToken OnPolicyUpdated()
    {
        if (_changeTokenSource.IsCancellationRequested)
        {
            _changeTokenSource = new();
            _changeToken = new(_changeTokenSource.Token);
        }

        return _changeToken;
    }

    /// <summary>
    /// When overriden produces compiled policy bundle stream.
    /// </summary>
    /// <param name="recompiling">
    /// <c>true</c> if it's first time bundle is compiled; otherwise <c>false</c>
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled policy bundle stream.</returns>
    [PublicAPI]
    protected abstract Task<Stream?> CompileBundleFromSource(bool recompiling, CancellationToken cancellationToken = default);

    protected internal async Task CompileBundle(bool recompiling, CancellationToken cancellationToken = default)
    {
        Logger.BundleCompiling();

        try
        {
            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

            var policy = await CompileBundleFromSource(recompiling, cancellationToken).ConfigureAwait(false);

            if (policy == null)
                return;

            await using var _ = policy.ConfigureAwait(false);

            var oldFactory = _factory;

            _factory = new OpaBundleEvaluatorFactory(
                policy,
                _options.CurrentValue.EngineOptions,
                ImportsAbiFactory.ImportsAbi,
                LoggerFactory
                );

            oldFactory?.Dispose();

            if (recompiling)
            {
                Logger.BundleRecompilationSucceeded();
                _changeTokenSource.Cancel();
            }
        }
        catch (Exception ex)
        {
            Logger.BundleCompilationFailed(ex);
            OpaEventSource.Log.BundleCompilationFailed();
            throw;
        }
        finally
        {
            _lock.Release();
        }

        Logger.BundleCompilationSucceeded();
        OpaEventSource.Log.BundleCompilationSucceeded();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing">If <c>true</c> method call comes from a Dispose method; otherwise <c>false</c>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _factory?.Dispose();
            _lock.Dispose();
            _changeTokenSource.Dispose();
        }
    }

    /// <inheritdoc />
    public virtual async Task StartAsync(CancellationToken cancellationToken)
    {
        await CompileBundle(false, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}