namespace OpaDotNet.Wasm;

public sealed class OpaBundleEvaluatorFactory : OpaEvaluatorFactory
{
    private readonly OpaPolicy _policy;

    private readonly WasmPolicyEngineOptions _options;

    public OpaBundleEvaluatorFactory(
        Stream bundleStream,
        WasmPolicyEngineOptions? options = null,
        Func<IOpaImportsAbi>? importsAbiFactory = null,
        ILoggerFactory? loggerFactory = null) : base(importsAbiFactory, loggerFactory)
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

    public override IOpaEvaluator Create()
    {
        return Create(_policy, _options);
    }
}