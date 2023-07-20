namespace OpaDotNet.Wasm;

public sealed class OpaWasmEvaluatorFactory : OpaEvaluatorFactoryBase
{
    private readonly OpaPolicy _policy;

    private readonly WasmPolicyEngineOptions _options;

    public OpaWasmEvaluatorFactory(
        Stream policyWasm,
        Func<IOpaImportsAbi>? importsAbi = null,
        ILoggerFactory? loggerFactory = null,
        WasmPolicyEngineOptions? options = null) : base(importsAbi, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(policyWasm);

        _options = options ?? WasmPolicyEngineOptions.Default;

        var buffer = new byte[policyWasm.Length];
        var bytesRead = policyWasm.Read(buffer);

        if (bytesRead < policyWasm.Length)
            throw new OpaRuntimeException("Failed to read wasm policy stream");

        _policy = new(buffer);
    }

    public override IOpaEvaluator Create()
    {
        return Create(_policy, _options);
    }
}