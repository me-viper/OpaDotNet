namespace OpaDotNet.Wasm.GoCompat;

[PublicAPI]
internal record X509ExtJson
{
    public bool Critical { get; set; }

    public HashSet<int>? Id { get; set; }

    public string? Value { get; set; }
}