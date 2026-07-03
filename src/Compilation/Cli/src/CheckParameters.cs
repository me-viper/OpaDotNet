using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Compilation.Cli;

/// <summary>
/// Check parameters.
/// </summary>
[PublicAPI]
public record CheckParameters
{
    /// <summary>
    /// Specifies if compilation source if file or bundle.
    /// </summary>
    public bool IsBundle { get; init; }

    /// <summary>
    /// Capabilities file that defines the built-in functions and other language features that policies may depend on.
    /// <see cref="CapabilitiesFilePath"/> overrides any other specified capabilities.
    /// </summary>
    public string? CapabilitiesFilePath { get; init; }

    /// <summary>
    /// Capabilities json that defines the built-in functions and other language features that policies may depend on.
    /// </summary>
    public ReadOnlyMemory<byte> CapabilitiesBytes { get; init; } = Memory<byte>.Empty;

    /// <summary>
    /// OPA capabilities version. If set, compiler will merge capabilities
    /// of specified version with any additional custom capabilities.
    /// </summary>
    public string? CapabilitiesVersion { get; init; }

    /// <summary>
    /// Output format.
    /// </summary>
    public CheckOutputFormat Format { get; init; }

    /// <summary>
    /// Set file and directory names to ignore during loading (e.g., '.*' excludes hidden files).
    /// </summary>
    public IReadOnlySet<string> Ignore { get; init; } = new HashSet<string>();

    /// <summary>
    /// Set the number of errors to allow before compilation fails early (default 10).
    /// </summary>
    public int? MaxErrors { get; init; }

    /// <summary>
    /// Enable compiler strict mode.
    /// </summary>
    public bool Strict { get; init; }

    /// <summary>
    /// Schema file path or directory path.
    /// </summary>
    public string? Schema { get; init; }

    /// <summary>
    /// Sets OPA features and behaviors that will be enabled by default.
    /// <b>V0</b> assumes <b>--v0-compatible</b> flag.
    /// <b>V0CompatV1</b> assumes <b>--v0-v1</b> flag.
    /// </summary>
    public RegoVersion RegoVersion { get; init; } = RegoVersion.V0;
}