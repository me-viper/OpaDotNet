using System.Buffers;

using OpaDotNet.Wasm.Internal;

namespace OpaDotNet.Wasm;

/// <summary>
/// A factory abstraction for a component that can create <see cref="IOpaEvaluator"/> instances from OPA policy WASM binary.
/// </summary>
[PublicAPI]
public sealed class OpaWasmEvaluatorFactory : IOpaEvaluatorFactory
{
    private readonly Func<IOpaEvaluator> _factory;

    private readonly Action _disposer;

    private bool _disposed;

    /// <summary>
    /// Creates new instance of <see cref="OpaWasmEvaluatorFactory"/>.
    /// </summary>
    /// <param name="source">OPA policy WASM binary stream</param>
    public OpaWasmEvaluatorFactory(Stream source) : this(source, WasmPolicyEngineOptions.Default)
    {
    }

    /// <summary>
    /// Creates new instance of <see cref="OpaWasmEvaluatorFactory"/>.
    /// </summary>
    /// <param name="source">OPA policy WASM binary stream</param>
    /// <param name="options">Evaluation engine options</param>
    public OpaWasmEvaluatorFactory(Stream source, WasmPolicyEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.CachePath))
        {
            var buffer = ArrayPool<byte>.Shared.Rent((int)source.Length);

            var bytesRead = source.Read(buffer);

            if (bytesRead < source.Length)
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
            source.CopyTo(fs);
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

    /// <summary>
    /// Creates new OPA evaluator instance
    /// </summary>
    /// <param name="source">OPA policy WASM binary stream</param>
    /// <param name="options">Evaluation engine options</param>
    /// <returns>New OPA evaluator instance</returns>
    public static IOpaEvaluator Create(Stream source, WasmPolicyEngineOptions? options = null)
    {
        using var result = new OpaWasmEvaluatorFactory(source, options ?? WasmPolicyEngineOptions.Default);
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