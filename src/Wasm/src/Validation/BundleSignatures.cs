using System.Text.Json.Serialization;

namespace OpaDotNet.Wasm.Validation;

internal class BundleSignatures
{
    [JsonPropertyName("signatures")]
    public List<string> Signatures { get; init; } = new();
}