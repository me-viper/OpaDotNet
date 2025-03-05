namespace OpaDotNet.Wasm.GoCompat;

[PublicAPI]
internal record X509NamesJson
{
    public HashSet<int>? Id { get; set; }

    public string? Value { get; set; }
}