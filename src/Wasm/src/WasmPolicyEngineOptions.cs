using System.Text.Encodings.Web;

using OpaDotNet.Wasm.Builtins;
using OpaDotNet.Wasm.Rego;

namespace OpaDotNet.Wasm;

/// <summary>
/// Contains members that affect OPA policy engine configuration.
/// </summary>
[PublicAPI]
public class WasmPolicyEngineOptions
{
    private Func<IOpaImportsAbi> _makeBuiltins;

    private readonly ImportsCache _importsCache = new();

    private bool _isReadOnly;

    /// <summary>
    /// Default engine options.
    /// </summary>
    public static WasmPolicyEngineOptions Default { get => new(); }

    /// <summary>
    /// Creates default engine options.
    /// </summary>
    /// <param name="options">JSON serialization options.</param>
    /// <returns>Engine options.</returns>
    public static WasmPolicyEngineOptions DefaultWithJsonOptions(Action<JsonSerializerOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var result = new WasmPolicyEngineOptions();
        options(result.SerializationOptions);
        return result;
    }

    private JsonSerializerOptions _jsonSerializationOptions = new()
    {
        Converters = { RegoSetJsonConverterFactory.Instance },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Creates new <see cref="WasmPolicyEngineOptions"/> instance.
    /// </summary>
    public WasmPolicyEngineOptions()
    {
        _makeBuiltins = DefaultBuiltins;
    }

    /// <summary>
    /// Minimal number of 64k pages available for WASM engine.
    /// </summary>
    public long MinMemoryPages
    {
        get;
        set
        {
            VerifyMutable();
            field = value;
        }
    } = 2;

    /// <summary>
    /// Maximum number of 64k pages available for WASM engine.
    /// </summary>
    public long? MaxMemoryPages
    {
        get;
        set
        {
            VerifyMutable();
            field = value;
        }
    }

    /// <summary>
    /// Max ABI versions to use.
    /// Can be useful for cases when you want evaluator to use lower ABI version than policy supports.
    /// </summary>
    public Version? MaxAbiVersion
    {
        get;
        set
        {
            VerifyMutable();
            field = value;
        }
    }

    /// <summary>
    /// Directory used to keep unpacked policies. If <c>null</c> policies will be kept in memory.
    /// </summary>
    /// <remarks>
    /// Directory must exist and requires write permissions.
    /// </remarks>
    public string? CachePath
    {
        get;
        set
        {
            VerifyMutable();
            field = value;
        }
    }

    /// <summary>
    /// If <c>true</c> errors in built-in functions will be threaded as exceptions that halt policy evaluation.
    /// </summary>
    public bool StrictBuiltinErrors
    {
        get;
        set
        {
            VerifyMutable();
            field = value;
        }
    }

    /// <summary>
    /// OPA bundle signature validation options.
    /// </summary>
    public SignatureValidationOptions SignatureValidation
    {
        get;
        set
        {
            VerifyMutable();
            field = value;
        }
    } = new();

    /// <summary>
    /// Maximum amount of time policy evaluation is allowed to run before it is cancelled.
    /// If <c>default</c> evaluation will not be limited by timeout.
    /// </summary>
    public TimeSpan Timeout
    {
        get;
        set
        {
            VerifyMutable();
            field = value;
        }
    }

    /// <summary>
    /// JSON serialization options.
    /// </summary>
    /// <exception cref="ArgumentNullException">Value is <c>null</c>.</exception>
    public JsonSerializerOptions SerializationOptions
    {
        get => _jsonSerializationOptions;
        set
        {
            VerifyMutable();

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            _jsonSerializationOptions = new(value)
            {
                Converters = { RegoSetJsonConverterFactory.Instance },
            };
        }
    }

    private IOpaImportsAbi DefaultBuiltins()
    {
        var bo = new WasmBuiltinsOptions();
        return new CompositeImportsHandler(
            bo.DefaultBuiltins,
            bo.CustomBuiltins.AsReadOnly(),
            _importsCache,
            Timeout > TimeSpan.Zero
            );
    }

    internal IOpaImportsAbi Builtins() => _makeBuiltins();

    internal void MakeReadOnly()
    {
        _isReadOnly = true;
    }

    internal void VerifyMutable()
    {
        if (_isReadOnly)
            throw new InvalidOperationException("This instance have been used to initialize engine and can't be mutated");
    }

    /// <summary>
    /// Configure OPA built-ins.
    /// </summary>
    public void ConfigureBuiltins(Action<WasmBuiltinsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        VerifyMutable();

        _makeBuiltins = () =>
        {
            var bo = new WasmBuiltinsOptions();
            configure(bo);
            return new CompositeImportsHandler(
                bo.DefaultBuiltins,
                bo.CustomBuiltins.AsReadOnly(),
                _importsCache,
                Timeout > TimeSpan.Zero
                );
        };
    }
}