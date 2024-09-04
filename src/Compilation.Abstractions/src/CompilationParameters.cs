using JetBrains.Annotations;

namespace OpaDotNet.Compilation.Abstractions;

/// <summary>
/// Compilation parameters.
/// </summary>
[PublicAPI]
public record CompilationParameters
{
    /// <summary>
    /// Specifies if compilation source if file or bundle.
    /// </summary>
    public bool IsBundle { get; set; }

    /// <summary>
    /// Which documents (entrypoints) will be queried when asking for policy decisions.
    /// </summary>
    public IReadOnlySet<string>? Entrypoints { get; set; }

    /// <summary>
    /// Capabilities file that defines the built-in functions and other language features that policies may depend on.
    /// <see cref="CapabilitiesFilePath"/> overrides any other specified capabilities.
    /// </summary>
    public string? CapabilitiesFilePath { get; set; }

    /// <summary>
    /// Capabilities json that defines the built-in functions and other language features that policies may depend on.
    /// </summary>
    [Obsolete("Use CompilationParameters.CapabilitiesBytes instead")]
    public Stream? CapabilitiesStream
    {
        get => CapabilitiesBytes.IsEmpty ? null : new MemoryStream(CapabilitiesBytes.ToArray());
        init
        {
            if (value == null)
                return;

            Memory<byte> buf = new byte[value.Length];
            _ = value.Read(buf.Span);
            CapabilitiesBytes = buf;
        }
    }

    /// <summary>
    /// Capabilities json that defines the built-in functions and other language features that policies may depend on.
    /// </summary>
    public ReadOnlyMemory<byte> CapabilitiesBytes { get; set; } = Memory<byte>.Empty;

    /// <summary>
    /// Output bundle revision.
    /// </summary>
    public string? Revision { get; set; }

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

    // BUG. Setting this to value > 0 crashes OPA compiler.
    // /// <summary>
    // /// Optimization level.
    // /// </summary>
    // public int OptimizationLevel { get; set; }

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