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

    private readonly ReaderWriterLockSlim _syncLock = new();

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

        _evaluatorPool = _poolProvider.Create(new OpaEvaluatorPoolPolicy(() => _factoryProvider.CreateEvaluator()));
        _recompilationMonitor = ChangeToken.OnChange(factoryProvider.OnPolicyUpdated, ResetPool);
    }

    private void ResetPool()
    {
        _logger.EvaluatorPoolResetting();

        _syncLock.EnterWriteLock();

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
            _syncLock.ExitWriteLock();
        }
    }

    public bool EvaluatePredicate<T>(T? input, string entrypoint)
    {
        ArgumentException.ThrowIfNullOrEmpty(entrypoint);

        _syncLock.EnterReadLock();

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
            _syncLock.ExitReadLock();
        }
    }

    public TOutput Evaluate<TInput, TOutput>(TInput input, string entrypoint) where TOutput : notnull
    {
        ArgumentException.ThrowIfNullOrEmpty(entrypoint);

        _syncLock.EnterReadLock();

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
            _syncLock.ExitReadLock();
        }
    }

    public string EvaluateRaw(ReadOnlySpan<char> inputJson, string entrypoint)
    {
        ArgumentException.ThrowIfNullOrEmpty(entrypoint);

        _syncLock.EnterReadLock();

        var pool = _evaluatorPool;
        var evaluator = pool.Get();

        try
        {
            return evaluator.EvaluateRaw(inputJson, entrypoint);
        }
        finally
        {
            pool.Return(evaluator);
            _syncLock.ExitReadLock();
        }
    }

    public void Dispose()
    {
        _recompilationMonitor.Dispose();

        if (_evaluatorPool is IDisposable d)
            d.Dispose();

        _syncLock.Dispose();
    }
}