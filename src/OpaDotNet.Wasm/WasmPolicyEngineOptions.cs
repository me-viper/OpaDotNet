namespace OpaDotNet.Wasm;

public class WasmPolicyEngineOptions
{
    public static WasmPolicyEngineOptions Default { get; } = new();
    
    private readonly JsonSerializerOptions _serializationOptions = JsonSerializerOptions.Default;
    
    public long MinMemoryPages { get; init; } = 2;
    
    public long? MaxMemoryPages { get; init; }

    public Version? MaxAbiVersion { get; init; }
    
    public JsonSerializerOptions SerializationOptions
    {
        get => _serializationOptions;
        init => _serializationOptions = value ?? throw new ArgumentNullException(nameof(value));
    }
}