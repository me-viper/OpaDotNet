using OpaDotNet.Wasm.Rego;

namespace OpaDotNet.Wasm;

/// <summary>
/// Contains members that affect OPA policy engine configuration.
/// </summary>
public class WasmPolicyEngineOptions
{
    /// <summary>
    /// Default engine options.
    /// </summary>
    public static WasmPolicyEngineOptions Default { get; } = new();

    private readonly JsonSerializerOptions _jsonSerializationOptions = new()
    {
        Converters = { RegoSetJsonConverterFactory.Instance },
    };

    /// <summary>
    /// Minimal number of 64k pages available for WASM engine.
    /// </summary>
    public long MinMemoryPages { get; init; } = 2;

    /// <summary>
    /// Maximum number of 64k pages available for WASM engine.
    /// </summary>
    public long? MaxMemoryPages { get; init; }

    /// <summary>
    /// Max ABI versions to use.
    /// Can be useful for cases when you want evaluator to use lower ABI version than policy supports.
    /// </summary>
    public Version? MaxAbiVersion { get; init; }

    /// <summary>
    /// Directory used to keep unpacked policies. If <c>null</c> policies will be kept in memory.
    /// </summary>
    /// <remarks>
    /// Directory must exist and requires write permissions.
    /// </remarks>
    public string? CachePath { get; init; }

    /// <summary>
    /// If <c>true</c> errors in built-in functions will be threaded as exceptions that halt policy evaluation.
    /// </summary>
    public bool StrictBuiltinErrors { get; init; }

    /// <summary>
    /// JSON serialization options.
    /// </summary>
    /// <exception cref="ArgumentNullException">Value is <c>null</c>.</exception>
    public JsonSerializerOptions SerializationOptions
    {
        get => _jsonSerializationOptions;
        init
        {
            _jsonSerializationOptions = value ?? throw new ArgumentNullException(nameof(value));
            _jsonSerializationOptions.Converters.Add(RegoSetJsonConverterFactory.Instance);
        }
    }
}