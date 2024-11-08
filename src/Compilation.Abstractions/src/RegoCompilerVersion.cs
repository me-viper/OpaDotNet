namespace OpaDotNet.Compilation.Abstractions;

/// <summary>
/// Compiler version information.
/// </summary>
[PublicAPI]
public record RegoCompilerVersion
{
    /// <summary>
    /// Version.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Commit.
    /// </summary>
    public string? Commit { get; set; }

    /// <summary>
    /// GO compiler version.
    /// </summary>
    public string? GoVersion { get; set; }

    /// <summary>
    /// Platform.
    /// </summary>
    public string? Platform { get; set; }
}