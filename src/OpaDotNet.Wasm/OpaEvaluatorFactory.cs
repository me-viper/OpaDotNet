using JetBrains.Annotations;

using Wasmtime;

namespace OpaDotNet.Wasm;

public abstract class OpaEvaluatorFactory
{
    private readonly ILoggerFactory _loggerFactory;

    private readonly Func<IOpaImportsAbi> _importsAbiFactory;

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
        return new OpaBundleEvaluatorFactory(policyBundle, options, importsAbiFactory, loggerFactory).Create();
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
        return new OpaWasmEvaluatorFactory(policyWasm, options, importsAbiFactory, loggerFactory).Create();
    }

    [PublicAPI]
    public abstract IOpaEvaluator Create();

    private protected IOpaEvaluator Create(
        OpaPolicy policy,
        WasmPolicyEngineOptions? options = null)
    {
        options ??= WasmPolicyEngineOptions.Default;

        var engine = new Engine();
        var linker = new Linker(engine);
        var store = new Store(engine);
        var memory = new Memory(store, options.MinMemoryPages, options.MaxMemoryPages);
        var module = Module.FromBytes(engine, "policy", policy.Policy.Span);

        var config = new WasmPolicyEngineConfiguration
        {
            Engine = engine,
            Linker = linker,
            Store = store,
            Memory = memory,
            Module = module,
            Imports = _importsAbiFactory(),
            Logger = _loggerFactory.CreateLogger<WasmOpaEvaluator>(),
            Options = options,
        };

        var result = new WasmOpaEvaluator(config);

        if (policy.Data != null)
            result.SetDataFromBytes(policy.Data.Value.Span);

        return result;
    }
}