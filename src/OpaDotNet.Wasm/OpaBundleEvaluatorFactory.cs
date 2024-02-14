using OpaDotNet.Wasm.Internal;

namespace OpaDotNet.Wasm;

/// <summary>
/// A factory abstraction for a component that can create <see cref="IOpaEvaluator"/> instances from OPA policy bundle.
/// </summary>
public sealed class OpaBundleEvaluatorFactory : OpaEvaluatorFactory
{
    private readonly Func<IOpaEvaluator> _factory;

    private readonly Action _disposer;

    /// <summary>
    /// Creates new instance of <see cref="OpaBundleEvaluatorFactory"/>.
    /// </summary>
    /// <param name="bundleStream">OPA policy bundle stream</param>
    /// <param name="options">Evaluation engine options</param>
    /// <param name="importsAbiFactory">Factory that produces instances of <see cref="IOpaImportsAbi"/></param>
    /// <param name="loggerFactory">Logger factory</param>
    public OpaBundleEvaluatorFactory(
        Stream bundleStream,
        WasmPolicyEngineOptions? options = null,
        Func<IOpaImportsAbi>? importsAbiFactory = null,
        ILoggerFactory? loggerFactory = null) : base(importsAbiFactory, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(bundleStream);

        options ??= WasmPolicyEngineOptions.Default;

        (_factory, _disposer) = string.IsNullOrWhiteSpace(options.CachePath)
            ? InMemoryFactory(bundleStream, options)
            : StreamFactory(bundleStream, options);
    }

    private (Func<IOpaEvaluator>, Action) InMemoryFactory(Stream bundleStream, WasmPolicyEngineOptions options)
    {
        OpaPolicy policy;

        try
        {
            policy = TarGzHelper.ReadBundleAndValidate(bundleStream, options.SignatureValidation);

            if (policy == null)
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

        IOpaEvaluator Factory() => Create(policy.Policy.Span, policy.Data.Span, options);

        return (Factory, () => { });
    }

    private (Func<IOpaEvaluator>, Action) StreamFactory(Stream bundleStream, WasmPolicyEngineOptions options)
    {
        try
        {
            var di = new DirectoryInfo(options.CachePath!);

            if (!di.Exists)
                throw new DirectoryNotFoundException($"Directory {di.FullName} was not found");

            var path = TarGzHelper.UnpackBundle(bundleStream, di, options.SignatureValidation);

            var policyFile = new FileInfo(Path.Combine(path.FullName, "policy.wasm"));

            if (!policyFile.Exists)
                throw new OpaRuntimeException("Bundle does not contain policy.wasm file");

            var dataFile = new FileInfo(Path.Combine(path.FullName, "data.json"));

            IOpaEvaluator Factory()
            {
                using var pfs = policyFile.OpenRead();
                using var dfs = dataFile.Exists ? dataFile.OpenRead() : null;
                return Create(pfs, dfs, options);
            }

            void Disposer() => path.Delete(true);

            return (Factory, Disposer);
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

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        _disposer();
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override IOpaEvaluator Create()
    {
        ThrowIfDisposed();
        return _factory();
    }
}