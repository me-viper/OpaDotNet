using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Extensions.AspNetCore;

/// <summary>
/// Specifies options for Opa policy compiler.
/// </summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public record RegoCompilerOptions
{
    /// <summary>
    /// If <c>true</c> bundle contents are resolved prior sending to compiler; otherwise compiler will resolve
    /// bundle contents by itself.
    /// </summary>
    /// <remarks>
    /// This option is less efficient but useful for cases when underlying compiler has troubles resolving bundle contents
    /// from the file system, specifically when symlinks are involved.
    /// </remarks>
    public bool ForceBundleWriter { get; set; }

    /// <summary>
    /// List of permitted policy entrypoints.
    /// </summary>
    public IReadOnlyList<string>? Entrypoints { get; set; }

    /// <summary>
    /// Capabilities file that defines the built-in functions and other language features that policies may depend on.
    /// If capabilities file is specified only it will suppress all other capabilities sources.
    /// </summary>
    public string? CapabilitiesFilePath { get; set; }

    // /// <summary>
    // /// Capabilities json that defines the built-in functions and other language features that policies may depend on.
    // /// </summary>
    // public ReadOnlyMemory<byte> CapabilitiesBytes { get; set; } = Memory<byte>.Empty;

    // /// <summary>
    // /// Output bundle revision.
    // /// </summary>
    // public string? Revision { get; set; }

    /// <summary>
    /// Path compiler will use to store intermediate compilation artifacts.
    /// </summary>
    /// <remarks>
    /// Directory must exist and requires write permissions.
    /// </remarks>
    public string? OutputPath { get; set; }

    /// <summary>
    /// OPA capabilities version. If set, compiler will merge capabilities
    /// of specified version with any additional custom capabilities.
    /// </summary>
    public string? CapabilitiesVersion { get; set; }

    /// <summary>
    /// If <c>true</c> compiler will log debug information; otherwise <c>false</c>;
    /// </summary>
    public bool Debug { get; set; }

    /// <summary>
    /// Exclude dependents of entrypoints.
    /// </summary>
    public bool PruneUnused { get; set; }

    /// <summary>
    /// Set file and directory names to ignore during loading (e.g., '.*' excludes hidden files).
    /// </summary>
    public IReadOnlySet<string> Ignore { get; set; } = new HashSet<string>();

    /// <summary>
    /// Sets OPA features and behaviors that will be enabled by default.
    /// </summary>
    public RegoVersion RegoVersion { get; set; } = RegoVersion.V0;

    /// <summary>
    /// Follow symlinks in the input set of paths when building the bundle.
    /// </summary>
    public bool FollowSymlinks { get; set; }

    /// <summary>
    /// If print statements are not enabled, calls to <c>print()</c> are erased at compile-time
    /// </summary>
    public bool DisablePrintStatements { get; set; }
}