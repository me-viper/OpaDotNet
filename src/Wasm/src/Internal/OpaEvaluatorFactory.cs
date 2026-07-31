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

        OpaWasmEvaluator result;

#pragma warning disable CA2000
        var cfg = new Config();
#pragma warning restore CA2000

        if (options.Timeout > TimeSpan.Zero)
            cfg.WithEpochInterruption(true);

        Engine? engine = null;
        Linker? linker = null;
        Store? store = null;
        Module? module = null;

        try
        {
            engine = new Engine(cfg);
            linker = new Linker(engine);
            store = new Store(engine);
            var memory = new Memory(store, options.MinMemoryPages, options.MaxMemoryPages);
            module = Module.FromStream(engine, "policy", policy);

            var config = new WasmPolicyEngineConfiguration
            {
                Engine = engine,
                Linker = linker,
                Store = store,
                Memory = memory,
                Module = module,
                Options = options,
                Imports = options.Builtins(),
                Timeout = options.Timeout,
            };

            options.MakeReadOnly();

            result = new OpaWasmEvaluator(config);
        }
        catch (Exception)
        {
            module?.Dispose();
            store?.Dispose();
            linker?.Dispose();
            engine?.Dispose();

            throw;
        }

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
        OpaWasmEvaluator result;

#pragma warning disable CA2000
        var cfg = new Config();
#pragma warning restore CA2000

        if (options.Timeout > TimeSpan.Zero)
            cfg.WithEpochInterruption(true);

        Engine? engine = null;
        Linker? linker = null;
        Store? store = null;
        Module? module = null;

        try
        {
            engine = new Engine(cfg);
            linker = new Linker(engine);
            store = new Store(engine);
            var memory = new Memory(store, options.MinMemoryPages, options.MaxMemoryPages);
            module = Module.FromBytes(engine, "policy", policy);

            var config = new WasmPolicyEngineConfiguration
            {
                Engine = engine,
                Linker = linker,
                Store = store,
                Memory = memory,
                Module = module,
                Options = options,
                Imports = options.Builtins(),
                Timeout = options.Timeout,
            };

            options.MakeReadOnly();

            result = new OpaWasmEvaluator(config);
        }
        catch (Exception)
        {
            module?.Dispose();
            store?.Dispose();
            linker?.Dispose();
            engine?.Dispose();

            throw;
        }

        if (!data.IsEmpty)
            result.SetDataFromBytes(data);

        return result;
    }
}