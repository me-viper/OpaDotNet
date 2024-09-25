namespace OpaDotNet.Wasm.GoCompat;

[PublicAPI]
internal record X509ExtJson
{
    public bool Critical { get; set; }

    public int[]? Id { get; set; }

    public string? Value { get; set; }
}