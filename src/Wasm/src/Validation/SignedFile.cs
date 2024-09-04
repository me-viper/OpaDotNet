using System.Text.Json.Serialization;

namespace OpaDotNet.Wasm.Validation;

internal record SignedFile
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("hash")]
    public string? Hash { get; init; }

    [JsonPropertyName("algorithm")]
    public string? Algorithm { get; init; }

    [JsonIgnore]
    internal bool IsValid { get; set; }
}