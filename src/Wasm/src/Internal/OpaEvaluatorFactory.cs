using Wasmtime;

namespace OpaDotNet.Wasm.Internal;

/// <summary>
/// Low-level component that can create <see cref="IOpaEvaluator"/> instances.
/// </summary>
internal static class OpaEvaluatorFactory
{
    internal static IOpaEvaluator Create(
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
            Options = options,
            Imports = options.Builtins(),
        };

        var result = new OpaWasmEvaluator(config);

        if (data != null)
            result.SetDataFromStream(data);

        return result;
    }

    internal static IOpaEvaluator Create(
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
            Options = options,
            Imports = options.Builtins(),
        };

        var result = new OpaWasmEvaluator(config);

        if (!data.IsEmpty)
            result.SetDataFromBytes(data);

        return result;
    }
}