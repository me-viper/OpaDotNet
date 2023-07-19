namespace OpaDotNet.Wasm;

public class OpaEvaluatorFactory : IOpaEvaluatorFactory
{
    private readonly ILoggerFactory _loggerFactory;

    private readonly Func<IOpaImportsAbi> _importsAbi;

    public OpaEvaluatorFactory(Func<IOpaImportsAbi>? importsAbiFactory = null, ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;;
        _importsAbi = importsAbiFactory ?? (static () => new DefaultOpaImportsAbi());;
    }

    /// <inheritdoc />
    public IOpaEvaluator CreateFromBundle(Stream policyBundle, WasmPolicyEngineOptions? options = null)
    {
        return new OpaBundleEvaluatorFactory(policyBundle, _importsAbi, _loggerFactory, options).Create();
    }

    /// <inheritdoc />
    public IOpaEvaluator CreateFromWasm(Stream policyWasm, WasmPolicyEngineOptions? options = null)
    {
        return new OpaWasmEvaluatorFactory(policyWasm, _importsAbi, _loggerFactory, options).Create();
    }
}