using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using Wasmtime;

namespace OpaDotNet.Wasm;

/// <summary>
/// A factory abstraction for a component that can create <see cref="IOpaEvaluator"/> instances.
/// </summary>
public abstract class OpaEvaluatorFactory : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;

    private readonly Func<IOpaImportsAbi> _importsAbiFactory;

    private bool _disposed;

    /// <summary>
    /// Creates new instance of <see cref="OpaEvaluatorFactory"/>.
    /// </summary>
    /// <param name="importsAbiFactory">Factory that produces instances of <see cref="IOpaImportsAbi"/></param>
    /// <param name="loggerFactory">Logger factory</param>
    protected OpaEvaluatorFactory(Func<IOpaImportsAbi>? importsAbiFactory, ILoggerFactory? loggerFactory)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _importsAbiFactory = importsAbiFactory ?? (static () => new DefaultOpaImportsAbi());
    }

    /// <summary>
    /// Creates evaluator from compiled policy bundle.
    /// </summary>
    /// <remarks>
    /// Loads policy (policy.wasm) and external data (data.json) from the bundle.
    /// </remarks>
    /// <param name="policyBundle">Compiled policy bundle (*.tar.gz)</param>
    /// <param name="options">Evaluator configuration</param>
    /// <param name="importsAbiFactory">Built-ins implementation factory</param>
    /// <param name="loggerFactory">Logger factory</param>
    /// <returns>Evaluator instance</returns>
    public static IOpaEvaluator CreateFromBundle(
        Stream policyBundle,
        WasmPolicyEngineOptions? options = null,
        Func<IOpaImportsAbi>? importsAbiFactory = null,
        ILoggerFactory? loggerFactory = null)
    {
        using var result = new OpaBundleEvaluatorFactory(policyBundle, options, importsAbiFactory, loggerFactory);
        return result.Create();
    }

    /// <summary>
    /// Creates evaluator from compiled wasm policy file.
    /// </summary>
    /// <remarks>
    /// If evaluator requires external data it should be loaded manually.
    /// </remarks>
    /// <param name="policyWasm">Compiled wasm policy file (*.wasm)</param>
    /// <param name="options">Evaluator configuration</param>
    /// <param name="importsAbiFactory">Built-ins implementation factory</param>
    /// <param name="loggerFactory">Logger factory</param>
    /// <returns>Evaluator instance</returns>
    public static IOpaEvaluator CreateFromWasm(
        Stream policyWasm,
        WasmPolicyEngineOptions? options = null,
        Func<IOpaImportsAbi>? importsAbiFactory = null,
        ILoggerFactory? loggerFactory = null)
    {
        using var result = new OpaWasmEvaluatorFactory(policyWasm, options, importsAbiFactory, loggerFactory);
        return result.Create();
    }

    /// <summary>
    /// Creates new instance of <see cref="IOpaEvaluator"/>.
    /// </summary>
    [PublicAPI]
    public abstract IOpaEvaluator Create();

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing">
    /// If disposing equals true, the method has been called directly or indirectly by a user's code.
    /// </param>
    [PublicAPI]
    protected virtual void Dispose(bool disposing)
    {
        _disposed = true;
    }

    /// <summary>
    /// Throws exception if this instance have been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    [ExcludeFromCodeCoverage]
    protected void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().ToString());
    }

    private protected IOpaEvaluator Create(
        Stream policy,
        Stream? data,
        WasmPolicyEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(options);

        var engine = new Engine();
        var linker = new Linker(engine);
        var store = new Store(engine);
        var memory = new Memory(store, options.MinMemoryPages, options.MaxMemoryPages);
        var module = Module.FromStream(engine, "policy", policy);

        var config = new WasmPolicyEngineConfiguration
        {
            Engine = engine,
            Linker = linker,
            Store = store,
            Memory = memory,
            Module = module,
            Imports = _importsAbiFactory(),
            Logger = _loggerFactory.CreateLogger<OpaWasmEvaluator>(),
            Options = options,
        };

        var result = new OpaWasmEvaluator(config);

        if (data != null)
            result.SetDataFromStream(data);

        return result;
    }

    private protected IOpaEvaluator Create(
        ReadOnlySpan<byte> policy,
        ReadOnlySpan<byte> data,
        WasmPolicyEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var engine = new Engine();
        var linker = new Linker(engine);
        var store = new Store(engine);
        var memory = new Memory(store, options.MinMemoryPages, options.MaxMemoryPages);
        var module = Module.FromBytes(engine, "policy", policy);

        var config = new WasmPolicyEngineConfiguration
        {
            Engine = engine,
            Linker = linker,
            Store = store,
            Memory = memory,
            Module = module,
            Imports = _importsAbiFactory(),
            Logger = _loggerFactory.CreateLogger<OpaWasmEvaluator>(),
            Options = options,
        };

        var result = new OpaWasmEvaluator(config);

        if (!data.IsEmpty)
            result.SetDataFromBytes(data);

        return result;
    }
}