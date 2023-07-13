using Wasmtime;

namespace OpaDotNet.Wasm;

public class OpaEvaluatorFactory : IOpaEvaluatorFactory
{
    private readonly ILoggerFactory _loggerFactory;

    private readonly Func<IOpaImportsAbi> _importsAbi;

    public OpaEvaluatorFactory(Func<IOpaImportsAbi>? importsAbiFactory = null, ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        _importsAbi = importsAbiFactory ?? (static () => new DefaultOpaImportsAbi());
    }

    private IOpaEvaluator Create(OpaPolicy policy, WasmPolicyEngineOptions? options = null)
    {
        options ??= WasmPolicyEngineOptions.Default;

        var engine = new Engine();
        var linker = new Linker(engine);
        var store = new Store(engine);
        var memory = new Memory(store, options.MinMemoryPages, options.MaxMemoryPages);
        var module = Module.FromStream(engine, "policy", policy.Policy);

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
        result.SetDataFromStream(policy.Data);
        return result;
    }

    /// <inheritdoc />
    public IOpaEvaluator CreateFromBundle(Stream policyBundle, WasmPolicyEngineOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(policyBundle);

        options ??= WasmPolicyEngineOptions.Default;

        OpaPolicy? policy = null;

        try
        {
            policy = TarGzHelper.ReadBundle(policyBundle);

            if (policy == null)
                throw new OpaRuntimeException("Failed to unpack policy bundle");

            return Create(policy, options);
        }
        catch (OpaRuntimeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OpaRuntimeException("Failed to unpack policy bundle", ex);
        }
        finally
        {
            policy?.Dispose();
        }
    }

    /// <inheritdoc />
    public IOpaEvaluator CreateFromWasm(Stream policyWasm, WasmPolicyEngineOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(policyWasm);

        return Create(new(policyWasm), options);
    }
}