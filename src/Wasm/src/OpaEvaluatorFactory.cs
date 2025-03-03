using System.Buffers;

using OpaDotNet.Wasm.Internal;

using Wasmtime;

namespace OpaDotNet.Wasm;

[PublicAPI]
public interface IOpaEvaluatorFactory
{
    IOpaEvaluator CreateFromWasm(Stream policyWasm);

    IOpaEvaluator CreateFromBundle(Stream bundle);
}

/// <summary>
/// A factory abstraction for a component that can create <see cref="IOpaEvaluator"/> instances.
/// </summary>
public class OpaEvaluatorFactory : IOpaEvaluatorFactory
{
    private WasmPolicyEngineOptions Options { get; }

    /// <summary>
    /// Creates new instance of <see cref="OpaEvaluatorFactory"/>.
    /// </summary>
    public OpaEvaluatorFactory() : this(WasmPolicyEngineOptions.Default)
    {
    }

    /// <summary>
    /// Creates new instance of <see cref="OpaEvaluatorFactory"/>.
    /// </summary>
    /// <param name="options">Evaluation engine options</param>
    public OpaEvaluatorFactory(WasmPolicyEngineOptions? options)
    {
        Options = options ?? WasmPolicyEngineOptions.Default;
    }

    internal IOpaEvaluator CreateFromWasm(Span<byte> policyWasm) => Create(policyWasm, Span<byte>.Empty, Options);

    public IOpaEvaluator CreateFromWasm(Stream policyWasm)
    {
        var buffer = ArrayPool<byte>.Shared.Rent((int)policyWasm.Length);

        try
        {
            var bytesRead = policyWasm.Read(buffer);

            if (bytesRead < policyWasm.Length)
                throw new OpaRuntimeException("Failed to read wasm policy stream");

            return Create(buffer.AsSpan(0, bytesRead), Span<byte>.Empty, Options);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public IOpaEvaluator CreateFromBundle(Stream bundle)
    {
        try
        {
            var policy = TarGzHelper.ReadBundleAndValidate(bundle, Options.SignatureValidation);

            if (policy == null)
                throw new OpaRuntimeException("Failed to unpack policy bundle");

            return Create(policy.Policy.Span, policy.Data.Span, Options);
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

    private IOpaEvaluator Create(
        Stream policy,
        Stream? data,
        WasmPolicyEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(options);

        var engine = new Engine();
        var linker = new Linker(engine);
        var store = new Store(engine);
        var memory = new Memory(store, options.MinMemoryPages, options.MaxMemoryPages);
        var module = Module.FromStream(engine, "policy", policy);

        var config = new WasmPolicyEngineConfiguration
        {
            Engine = engine,
            Linker = linker,
            Store = store,
            Memory = memory,
            Module = module,
            Options = options,
            Imports = options.Builtins(),
        };

        var result = new OpaWasmEvaluator(config);

        if (data != null)
            result.SetDataFromStream(data);

        return result;
    }

    private IOpaEvaluator Create(
        ReadOnlySpan<byte> policy,
        ReadOnlySpan<byte> data,
        WasmPolicyEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var engine = new Engine();
        var linker = new Linker(engine);
        var store = new Store(engine);
        var memory = new Memory(store, options.MinMemoryPages, options.MaxMemoryPages);
        var module = Module.FromBytes(engine, "policy", policy);

        var config = new WasmPolicyEngineConfiguration
        {
            Engine = engine,
            Linker = linker,
            Store = store,
            Memory = memory,
            Module = module,
            Options = options,
            Imports = options.Builtins(),
        };

        var result = new OpaWasmEvaluator(config);

        if (!data.IsEmpty)
            result.SetDataFromBytes(data);

        return result;
    }
}