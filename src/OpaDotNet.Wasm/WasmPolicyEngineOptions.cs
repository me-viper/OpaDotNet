namespace OpaDotNet.Wasm;

public class WasmPolicyEngineOptions
{
    /// <summary>
    /// Default engine options.
    /// </summary>
    public static WasmPolicyEngineOptions Default { get; } = new();

    private readonly JsonSerializerOptions _serializationOptions = JsonSerializerOptions.Default;

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
    /// Json serialization options.
    /// </summary>
    /// <exception cref="ArgumentNullException">Value is null</exception>
    public JsonSerializerOptions SerializationOptions
    {
        get => _serializationOptions;
        init => _serializationOptions = value ?? throw new ArgumentNullException(nameof(value));
    }
}