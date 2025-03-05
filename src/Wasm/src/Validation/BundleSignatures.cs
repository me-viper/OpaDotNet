using System.Text.Json.Serialization;

namespace OpaDotNet.Wasm.Validation;

internal class BundleSignatures
{
    [UsedImplicitly]
    [JsonPropertyName("signatures")]
    public List<string> Signatures { get; init; } = new();
}