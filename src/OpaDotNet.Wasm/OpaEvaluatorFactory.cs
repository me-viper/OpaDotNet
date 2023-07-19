namespace OpaDotNet.Wasm;

public class OpaEvaluatorFactory : OpaEvaluatorFactoryBase, IOpaEvaluatorFactory
{
    public OpaEvaluatorFactory(Func<IOpaImportsAbi>? importsAbiFactory = null, ILoggerFactory? loggerFactory = null)
        : base(importsAbiFactory, loggerFactory)
    {
    }

    /// <inheritdoc />
    public IOpaEvaluator CreateFromBundle(Stream policyBundle, WasmPolicyEngineOptions? options = null)
    {
        return new OpaBundleEvaluatorFactory(policyBundle, ImportsAbi, LoggerFactory, options).Create();
    }

    /// <inheritdoc />
    public IOpaEvaluator CreateFromWasm(Stream policyWasm, WasmPolicyEngineOptions? options = null)
    {
        return new OpaWasmEvaluatorFactory(policyWasm, ImportsAbi, LoggerFactory, options).Create();
    }
}