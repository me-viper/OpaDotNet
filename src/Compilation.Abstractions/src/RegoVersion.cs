namespace OpaDotNet.Compilation.Abstractions;

/// <summary>
/// Language and runtime compatibility.
/// </summary>
[PublicAPI]
public enum RegoVersion
{
    /// <summary>
    /// Pre OPA v1.0 release.
    /// </summary>
    V0 = 0,

    /// <summary>
    /// Requires modules to comply with both the V0 and V1 syntax
    /// (as when 'rego.v1' is imported in a module).
    /// </summary>
    V0CompatV1,

    /// <summary>
    /// OPA v1.0 release.
    /// </summary>
    V1,
}