namespace OpaDotNet.Wasm;

/// <summary>
/// A factory abstraction for a component that can create <see cref="IOpaEvaluator"/> instances from OPA policy WASM binary.
/// </summary>
public sealed class OpaWasmEvaluatorFactory : OpaEvaluatorFactory
{
    private readonly Func<IOpaEvaluator> _factory;

    private readonly Action _disposer;

    /// <summary>
    /// Creates new instance of <see cref="OpaWasmEvaluatorFactory"/>.
    /// </summary>
    /// <param name="policyWasm">OPA policy WASM binary stream</param>
    /// <param name="options">Evaluation engine options</param>
    /// <param name="importsAbiFactory">Factory that produces instances of <see cref="IOpaImportsAbi"/></param>
    /// <param name="loggerFactory">Logger factory</param>
    public OpaWasmEvaluatorFactory(
        Stream policyWasm,
        WasmPolicyEngineOptions? options = null,
        Func<IOpaImportsAbi>? importsAbiFactory = null,
        ILoggerFactory? loggerFactory = null) : base(importsAbiFactory, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(policyWasm);

        options ??= WasmPolicyEngineOptions.Default;

        if (string.IsNullOrWhiteSpace(options.CachePath))
        {
            var buffer = new byte[policyWasm.Length];
            var bytesRead = policyWasm.Read(buffer);

            if (bytesRead < policyWasm.Length)
                throw new OpaRuntimeException("Failed to read wasm policy stream");

            _factory = () => Create(buffer, Memory<byte>.Empty.Span, options);
            _disposer = () => { };
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
                return Create(policyFs, null, options);
            };

            _disposer = () => cache.Delete(true);
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