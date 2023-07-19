using Wasmtime;

namespace OpaDotNet.Wasm;

public class OpaEvaluatorFactoryBase
{
    protected readonly ILoggerFactory LoggerFactory;

    protected readonly Func<IOpaImportsAbi> ImportsAbi;

    private protected OpaEvaluatorFactoryBase(Func<IOpaImportsAbi>? importsAbiFactory, ILoggerFactory? loggerFactory)
    {
        LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        ImportsAbi = importsAbiFactory ?? (static () => new DefaultOpaImportsAbi());
    }

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
            Imports = ImportsAbi(),
            Logger = LoggerFactory.CreateLogger<WasmOpaEvaluator>(),
            Options = options,
        };

        var result = new WasmOpaEvaluator(config);

        if (policy.Data != null)
            result.SetDataFromBytes(policy.Data.Value.Span);

        return result;
    }
}