using JetBrains.Annotations;

namespace OpaDotNet.Wasm.GoCompat;

[PublicAPI]
internal record X509NamesJson
{
    public int[]? Id { get; set; }

    public string? Value { get; set; }
}