namespace OpaDotNet.Wasm.Compilation;

internal record OpaCliVersion
{
    public string? Version { get; set; }

    public string? Commit { get; set; }

    public string? Timestamp { get; set; }

    public string? GoVersion { get; set; }

    public string? Platform { get; set; }

    public string? WebAssembly { get; set; }
}