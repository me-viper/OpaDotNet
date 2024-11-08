using System.Text.Json.Serialization;

namespace OpaDotNet.Compilation.Abstractions;

/// <summary>
/// Policy bundle manifest.
/// </summary>
[PublicAPI]
public record BundleManifest
{
    /// <summary>
    /// If the bundle service is capable of serving different revisions of the same bundle,
    /// the service should include a top-level revision field containing a string value that identifies the bundle revision.
    /// </summary>
    [JsonPropertyName("revision")]
    public string? Revision { get; init; }

    /// <summary>
    /// If you expect to load additional data into OPA from outside the bundle (e.g., via OPA’s HTTP API)
    /// you should include a top-level roots field containing of path prefixes that declare the scope of the bundle.
    /// If the roots field is not included in the manifest it defaults to [""] which means that ALL data
    /// and policy must come from the bundle.
    /// </summary>
    [JsonPropertyName("roots")]
    public HashSet<string>? Roots { get; init; }

    /// <summary>
    /// A list of OPA WebAssembly (Wasm) module files in the bundle along with metadata for how they should be evaluated.
    /// </summary>
    [JsonPropertyName("wasm")]
    public HashSet<WasmMetadata> Wasm { get; } = new();

    /// <summary>
    /// An optional key that contains arbitrary metadata to accompany the bundle.
    /// This metadata is available for querying using data.system, along with the rest of the manifest.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; } = new();
}

/// <summary>
/// OPA WebAssembly (Wasm) module files in the bundle.
/// </summary>
/// <param name="Entrypoint">
/// A string path defining what query path the wasm module is built to evaluate.
/// Once loaded any usage of this path in a query will use the Wasm module to compute the value.
/// </param>
/// <param name="Module">A string path to the Wasm module relative to the root of the bundle.</param>
[PublicAPI]
public record WasmMetadata(
    [property: JsonPropertyName("entrypoint")]
    string Entrypoint,
    [property: JsonPropertyName("module")] string Module
    );