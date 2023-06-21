using Wasmtime;

namespace OpaDotNet.Wasm;

public class OpaEvaluatorFactory : IOpaEvaluatorFactory
{
    private readonly ILoggerFactory _loggerFactory;

    private readonly IOpaImportsAbi _importsAbi;

    public OpaEvaluatorFactory(IOpaImportsAbi? importsAbi = null, ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _importsAbi = importsAbi ?? new DefaultOpaImportsAbi(_loggerFactory.CreateLogger<DefaultOpaImportsAbi>());
    }

    public IOpaEvaluator CreateWithData<TData>(Stream policy, TData? data, WasmPolicyEngineOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(policy);

        string? dataJson = null;

        if (data != null)
        {
            options ??= WasmPolicyEngineOptions.Default;
            dataJson = JsonSerializer.Serialize<TData>(data, options.SerializationOptions);
        }

        return CreateWithJsonData(policy, dataJson, options);
    }

    public IOpaEvaluator CreateWithJsonData(Stream policy, string? dataJson = null, WasmPolicyEngineOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(policy);

        options ??= WasmPolicyEngineOptions.Default;

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
            Imports = _importsAbi,
            Logger = _loggerFactory.CreateLogger<WasmOpaEvaluator>(),
            Options = options,
        };

        var result = new WasmOpaEvaluator(config);

        result.UpdateData(dataJson);

        return result;
    }
}