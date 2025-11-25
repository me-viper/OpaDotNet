using DotNext.Threading;

using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

using OpaDotNet.Extensions.AspNetCore.Telemetry;
using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore;

internal class PooledOpaPolicyService : IOpaPolicyService, IDisposable
{
    private ObjectPool<IOpaEvaluator> _evaluatorPool;

    private readonly ILogger _logger;

    private readonly IDisposable _recompilationMonitor;

    private readonly OpaEvaluatorPoolProvider _poolProvider;

    private readonly IOpaPolicySource _factoryProvider;

    private readonly AsyncReaderWriterLock _syncLock = new();

    private readonly SemaphoreSlim? _concurrencyLock;

    public PooledOpaPolicyService(
        IOpaPolicySource factoryProvider,
        IOptions<OpaAuthorizationOptions> options,
        OpaEvaluatorPoolProvider poolProvider,
        ILogger<PooledOpaPolicyService> logger)
    {
        ArgumentNullException.ThrowIfNull(factoryProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(poolProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _factoryProvider = factoryProvider;
        _poolProvider = poolProvider;
        _logger = logger;

        _poolProvider.MaximumRetained = options.Value.MaximumEvaluatorsRetained;

        if (options.Value.MaximumEvaluators > 0)
            _concurrencyLock = new(options.Value.MaximumEvaluators, options.Value.MaximumEvaluators);

        _evaluatorPool = _poolProvider.Create(new OpaEvaluatorPoolPolicy(() => _factoryProvider.CreateEvaluator()));
        _recompilationMonitor = ChangeToken.OnChange(factoryProvider.OnPolicyUpdated, ResetPool);
    }

    private void ResetPool()
    {
        _logger.EvaluatorPoolResetting();

        var gotLock = _syncLock.TryEnterWriteLock(TimeSpan.FromSeconds(60));

        if (!gotLock)
            throw new TimeoutException("Failed to enter write lock within 60 seconds timeout");

        try
        {
            var oldPool = _evaluatorPool;
            _evaluatorPool = _poolProvider.Create(new OpaEvaluatorPoolPolicy(() => _factoryProvider.CreateEvaluator()));

            if (oldPool is not IDisposable pool)
            {
                _logger.EvaluatorPoolNotDisposable();
                return;
            }


            _logger.EvaluatorPoolDisposing();
            pool.Dispose();
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private Task ConcurrencyLockWaitAsync(CancellationToken cancellationToken)
        => _concurrencyLock?.WaitAsync(cancellationToken) ?? Task.CompletedTask;

    public async ValueTask<bool> EvaluatePredicate<T>(T input, string entrypoint, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(entrypoint);

        await _syncLock.EnterReadLockAsync(cancellationToken).ConfigureAwait(false);
        await ConcurrencyLockWaitAsync(cancellationToken).ConfigureAwait(false);

        var pool = _evaluatorPool;
        var evaluator = pool.Get();

        try
        {
            var result = evaluator.EvaluatePredicate(input, entrypoint);
            return result.Result;
        }
        finally
        {
            pool.Return(evaluator);
            _concurrencyLock?.Release();
            _syncLock.Release();
        }
    }

    public async ValueTask<TOutput> Evaluate<TInput, TOutput>(TInput input, string entrypoint, CancellationToken cancellationToken)
        where TOutput : notnull
    {
        ArgumentException.ThrowIfNullOrEmpty(entrypoint);

        await _syncLock.EnterReadLockAsync(cancellationToken).ConfigureAwait(false);
        await ConcurrencyLockWaitAsync(cancellationToken).ConfigureAwait(false);

        var pool = _evaluatorPool;
        var evaluator = pool.Get();

        try
        {
            var result = evaluator.Evaluate<TInput, TOutput>(input, entrypoint);
            return result.Result;
        }
        finally
        {
            pool.Return(evaluator);
            _concurrencyLock?.Release();
            _syncLock.Release();
        }
    }

    public async ValueTask<string> EvaluateRaw(ReadOnlyMemory<char> inputJson, string entrypoint, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(entrypoint);

        await _syncLock.EnterReadLockAsync(cancellationToken).ConfigureAwait(false);
        await ConcurrencyLockWaitAsync(cancellationToken).ConfigureAwait(false);

        var pool = _evaluatorPool;
        var evaluator = pool.Get();

        try
        {
            return evaluator.EvaluateRaw(inputJson.Span, entrypoint);
        }
        finally
        {
            pool.Return(evaluator);
            _concurrencyLock?.Release();
            _syncLock.Release();
        }
    }

    public void Dispose()
    {
        _recompilationMonitor.Dispose();

        if (_evaluatorPool is IDisposable d)
            d.Dispose();

        _concurrencyLock?.Dispose();
        _syncLock.Dispose();
    }
}