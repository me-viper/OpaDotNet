using OpaDotNet.Wasm.Internal;

namespace OpaDotNet.Wasm;

/// <summary>
/// A factory abstraction for a component that can create <see cref="IOpaEvaluator"/> instances from OPA policy bundle.
/// </summary>
[PublicAPI]
public sealed class OpaBundleEvaluatorFactory : IOpaEvaluatorFactory
{
    private readonly Func<IOpaEvaluator> _factory;

    private readonly Action _disposer;

    private bool _disposed;

    /// <summary>
    /// Creates new instance of <see cref="OpaBundleEvaluatorFactory"/>.
    /// </summary>
    /// <param name="source">OPA policy bundle stream</param>
    public OpaBundleEvaluatorFactory(Stream source) : this(source, WasmPolicyEngineOptions.Default)
    {
    }

    /// <summary>
    /// Creates new instance of <see cref="OpaBundleEvaluatorFactory"/>.
    /// </summary>
    /// <param name="source">OPA policy bundle stream</param>
    /// <param name="options">Evaluation engine options</param>
    public OpaBundleEvaluatorFactory(Stream source, WasmPolicyEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);

        (_factory, _disposer) = string.IsNullOrWhiteSpace(options.CachePath)
            ? InMemoryFactory(source, options)
            : StreamFactory(source, options);
    }

    private static (Func<IOpaEvaluator>, Action) InMemoryFactory(Stream bundleStream, WasmPolicyEngineOptions options)
    {
        var policy = UnpackBundle(bundleStream, options);
        return (() => OpaEvaluatorFactory.Create(policy.Policy.Span, policy.Data.Span, options), () => { });
    }

    private static (Func<IOpaEvaluator>, Action) StreamFactory(Stream bundleStream, WasmPolicyEngineOptions options)
    {
        try
        {
            var di = new DirectoryInfo(options.CachePath!);

            if (!di.Exists)
                throw new DirectoryNotFoundException($"Directory {di.FullName} was not found");

            var cache = new DirectoryInfo(Path.Combine(di.FullName, Guid.NewGuid().ToString()));
            cache.Create();

            using var fs = new FileStream(Path.Combine(cache.FullName, "bundle.tar.gz"), FileMode.CreateNew);
            bundleStream.CopyTo(fs);
            fs.Flush();

            var policyFilePath = fs.Name;

            IOpaEvaluator Factory()
            {
                using var policyFs = File.OpenRead(policyFilePath);
                var policy = UnpackBundle(policyFs, options);
                return OpaEvaluatorFactory.Create(policy.Policy.Span, policy.Data.Span, options);
            }

            void Disposer() => cache.Delete(true);

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

    private static OpaPolicy UnpackBundle(Stream policyBundle, WasmPolicyEngineOptions options)
    {
        try
        {
            var policy = TarGzHelper.ReadBundleAndValidate(policyBundle, options.SignatureValidation);

            if (policy == null)
                throw new OpaRuntimeException("Failed to unpack policy bundle");

            return policy;
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

    /// <summary>
    /// Creates new OPA evaluator instance
    /// </summary>
    /// <param name="source">OPA policy WASM binary stream</param>
    /// <param name="options">Evaluation engine options</param>
    /// <returns>New OPA evaluator instance</returns>
    public static IOpaEvaluator Create(Stream source, WasmPolicyEngineOptions? options = null)
    {
        using var result = new OpaBundleEvaluatorFactory(source, options ?? WasmPolicyEngineOptions.Default);
        return result.Create();
    }

    /// <inheritdoc />
    public IOpaEvaluator Create()
    {
        ThrowIfDisposed();
        return _factory();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposer();
        _disposed = true;
    }

    /// <summary>
    /// Throws exception if this instance have been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    [ExcludeFromCodeCoverage]
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().ToString());
    }
}