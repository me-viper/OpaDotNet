using JetBrains.Annotations;

using Wasmtime;

namespace OpaDotNet.Wasm;

public abstract class OpaEvaluatorFactoryBase
{
    private readonly ILoggerFactory _loggerFactory;

    private readonly Func<IOpaImportsAbi> _importsAbi;

    protected OpaEvaluatorFactoryBase(Func<IOpaImportsAbi>? importsAbiFactory, ILoggerFactory? loggerFactory)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _importsAbi = importsAbiFactory ?? (static () => new DefaultOpaImportsAbi());
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
            Imports = _importsAbi(),
            Logger = _loggerFactory.CreateLogger<WasmOpaEvaluator>(),
            Options = options,
        };

        var result = new WasmOpaEvaluator(config);

        if (policy.Data != null)
            result.SetDataFromBytes(policy.Data.Value.Span);

        return result;
    }
}