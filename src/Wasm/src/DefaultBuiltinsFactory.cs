using OpaDotNet.Wasm.Builtins;

namespace OpaDotNet.Wasm;

/// <summary>
/// Default built-ins factory.
/// </summary>
[PublicAPI]
public class DefaultBuiltinsFactory : IBuiltinsFactory
{
    private readonly ImportsCache _importsCache;

    private readonly Func<IOpaImportsAbi> _defaultBuiltins;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultBuiltinsFactory"/>.
    /// </summary>
    public DefaultBuiltinsFactory() : this(null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultBuiltinsFactory"/>.
    /// </summary>
    /// <param name="defaultBuiltins">Factory that produces instances of <see cref="IOpaImportsAbi"/></param>
    public DefaultBuiltinsFactory(Func<IOpaImportsAbi>? defaultBuiltins) : this(null, defaultBuiltins)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultBuiltinsFactory"/>.
    /// </summary>
    /// <param name="options">Evaluation engine options</param>
    public DefaultBuiltinsFactory(WasmPolicyEngineOptions options) : this(options, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultBuiltinsFactory"/>.
    /// </summary>
    /// <param name="options">Evaluation engine options</param>
    /// <param name="defaultBuiltins">Factory that produces instances of <see cref="IOpaImportsAbi"/></param>
    public DefaultBuiltinsFactory(WasmPolicyEngineOptions? options, Func<IOpaImportsAbi>? defaultBuiltins)
    {
        _defaultBuiltins = defaultBuiltins ?? (static () => new DefaultOpaImportsAbi());

        var opts = options ?? WasmPolicyEngineOptions.Default;
        _importsCache = new ImportsCache();
    }

    /// <summary>
    /// Custom built-ins.
    /// </summary>
    public IReadOnlyList<Func<IOpaCustomBuiltins>> CustomBuiltins { get; init; } = [];

    /// <summary>
    /// Creates built-ins implementation instances.
    /// </summary>
    public IOpaImportsAbi Create()
    {
        return new CompositeImportsHandler(
            _defaultBuiltins(),
            CustomBuiltins.Select(p => p()).ToList(),
            _importsCache
            );
    }
}