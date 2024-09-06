using OpaDotNet.Wasm.Builtins;

namespace OpaDotNet.Wasm;

[PublicAPI]
public class DefaultBuiltinsFactory : IBuiltinsFactory
{
    private readonly ImportsCache _importsCache;

    private readonly Func<IOpaImportsAbi> _defaultBuiltins;

    public DefaultBuiltinsFactory() : this(null, null)
    {
    }

    public DefaultBuiltinsFactory(Func<IOpaImportsAbi>? defaultBuiltins) : this(null, defaultBuiltins)
    {
    }

    public DefaultBuiltinsFactory(WasmPolicyEngineOptions options) : this(options, null)
    {
    }

    public DefaultBuiltinsFactory(WasmPolicyEngineOptions? options, Func<IOpaImportsAbi>? defaultBuiltins)
    {
        _defaultBuiltins = defaultBuiltins ?? (static () => new DefaultOpaImportsAbi());

        var opts = options ?? WasmPolicyEngineOptions.Default;
        _importsCache = new ImportsCache(opts.SerializationOptions);
    }

    public IReadOnlyList<Func<IOpaCustomBuiltins>> CustomBuiltins { get; init; } = [];

    public IOpaImportsAbi Create()
    {
        return new CompositeImportsHandler(
            _defaultBuiltins(),
            CustomBuiltins.Select(p => p()).ToList(),
            _importsCache
            );
    }
}