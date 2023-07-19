namespace OpaDotNet.Wasm;

public sealed class OpaBundleEvaluatorFactory : OpaEvaluatorFactoryBase
{
    private readonly OpaPolicy _policy;

    private readonly WasmPolicyEngineOptions _options;

    public OpaBundleEvaluatorFactory(
        Stream bundleStream,
        Func<IOpaImportsAbi>? importsAbi = null,
        ILoggerFactory? loggerFactory = null,
        WasmPolicyEngineOptions? options = null) : base(importsAbi, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(bundleStream);
        
        _options = options ?? WasmPolicyEngineOptions.Default;

        try
        {
            _policy = TarGzHelper.ReadBundle(bundleStream);

            if (_policy == null)
                throw new OpaRuntimeException("Failed to unpack policy bundle");
        }
        catch (OpaRuntimeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new OpaRuntimeException("Failed to unpack policy bundle", ex);
        }
    }

    public IOpaEvaluator Create()
    {
        return Create(_policy, _options);
    }
}