using System.Buffers;

namespace OpaDotNet.Wasm;

/// <summary>
/// A factory abstraction for a component that can create <see cref="IOpaEvaluator"/> instances from OPA policy bundle.
/// </summary>
public sealed class OpaBundleEvaluatorFactory : IDisposable
{
    private readonly Func<IOpaEvaluator> _factory;

    private readonly Action _disposer;

    private bool _disposed;

    /// <summary>
    /// Creates new instance of <see cref="OpaBundleEvaluatorFactory"/>.
    /// </summary>
    /// <param name="bundleStream">OPA policy bundle stream</param>
    /// <param name="options">Evaluation engine options</param>
    public OpaBundleEvaluatorFactory(Stream bundleStream, WasmPolicyEngineOptions? options)
    {
        ArgumentNullException.ThrowIfNull(bundleStream);
        options ??= WasmPolicyEngineOptions.Default;

        (_factory, _disposer) = string.IsNullOrWhiteSpace(options.CachePath)
            ? InMemoryFactory(bundleStream, options)
            : StreamFactory(bundleStream, options);
    }

    private (Func<IOpaEvaluator>, Action) InMemoryFactory(Stream bundleStream, WasmPolicyEngineOptions options)
    {
        var opaEvaluatorFactory = new OpaEvaluatorFactory(options);
        var buffer = ArrayPool<byte>.Shared.Rent((int)bundleStream.Length);
        var bytesRead = bundleStream.Read(buffer);

        if (bytesRead < bundleStream.Length)
            throw new OpaRuntimeException("Failed to read policy bundle stream");

        IOpaEvaluator Factory()
        {
            using var ms = new MemoryStream(buffer);
            return opaEvaluatorFactory.CreateFromBundle(ms);
        }

        return (Factory, () => ArrayPool<byte>.Shared.Return(buffer));
    }

    private (Func<IOpaEvaluator>, Action) StreamFactory(Stream bundleStream, WasmPolicyEngineOptions options)
    {
        try
        {
            var opaEvaluatorFactory = new OpaEvaluatorFactory(options);

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
                return opaEvaluatorFactory.CreateFromBundle(policyFs);
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

    public static IOpaEvaluator Create(Stream policyBundle, WasmPolicyEngineOptions? options = null)
    {
        using var result = new OpaBundleEvaluatorFactory(policyBundle, options);
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