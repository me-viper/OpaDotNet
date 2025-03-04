using System.Buffers;

namespace OpaDotNet.Wasm;

/// <summary>
/// A factory abstraction for a component that can create <see cref="IOpaEvaluator"/> instances from OPA policy WASM binary.
/// </summary>
public sealed class OpaWasmEvaluatorFactory : IOpaEvaluatorFactory
{
    private readonly Func<IOpaEvaluator> _factory;

    private readonly Action _disposer;

    private bool _disposed;

    /// <summary>
    /// Creates new instance of <see cref="OpaWasmEvaluatorFactory"/>.
    /// </summary>
    /// <param name="policyWasm">OPA policy WASM binary stream</param>
    public OpaWasmEvaluatorFactory(Stream policyWasm) : this(policyWasm, WasmPolicyEngineOptions.Default)
    {
    }

    /// <summary>
    /// Creates new instance of <see cref="OpaWasmEvaluatorFactory"/>.
    /// </summary>
    /// <param name="policyWasm">OPA policy WASM binary stream</param>
    /// <param name="options">Evaluation engine options</param>
    public OpaWasmEvaluatorFactory(Stream policyWasm, WasmPolicyEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(policyWasm);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.CachePath))
        {
            var buffer = ArrayPool<byte>.Shared.Rent((int)policyWasm.Length);

            var bytesRead = policyWasm.Read(buffer);

            if (bytesRead < policyWasm.Length)
                throw new OpaRuntimeException("Failed to read wasm policy stream");

            _factory = () => OpaEvaluatorFactory.Create(buffer.AsSpan(0, bytesRead), ReadOnlySpan<byte>.Empty, options);
            _disposer = () => ArrayPool<byte>.Shared.Return(buffer);
        }
        else
        {
            var di = new DirectoryInfo(options.CachePath!);

            if (!di.Exists)
                throw new DirectoryNotFoundException($"Directory {di.FullName} was not found");

            var cache = new DirectoryInfo(Path.Combine(di.FullName, Guid.NewGuid().ToString()));
            cache.Create();

            using var fs = new FileStream(Path.Combine(cache.FullName, "policy.wasm"), FileMode.CreateNew);
            policyWasm.CopyTo(fs);
            fs.Flush();

            var policyFilePath = fs.Name;

            _factory = () =>
            {
                using var policyFs = File.OpenRead(policyFilePath);
                return OpaEvaluatorFactory.Create(policyFs, null, options);
            };

            _disposer = () => cache.Delete(true);
        }
    }

    public static IOpaEvaluator Create(Stream policyWasm, WasmPolicyEngineOptions? options = null)
    {
        using var result = new OpaWasmEvaluatorFactory(policyWasm, options ?? WasmPolicyEngineOptions.Default);
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