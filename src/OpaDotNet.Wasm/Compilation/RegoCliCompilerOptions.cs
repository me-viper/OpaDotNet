using JetBrains.Annotations;

namespace OpaDotNet.Wasm.Compilation;

/// <summary>
/// Contains members that affect compiler behaviour.
/// </summary>
[PublicAPI]
public class RegoCliCompilerOptions
{
    /// <summary>
    /// Full path to opa cli tool.
    /// </summary>
    public string? OpaToolPath { get; set; }

    /// <summary>
    /// Path compiler will use to store intermediate compilation artifacts.
    /// </summary>
    /// <remarks>
    /// Directory must exist and requires write permissions.
    /// </remarks>
    public string? OutputPath { get; set; }

    /// <summary>
    /// Extra arguments to pass to <c>opa build</c> cli tool.
    /// </summary>
    public string? ExtraArguments { get; set; }

    /// <summary>
    /// OPA capabilities version. If set, compiler will merge capabilities
    /// of specified version with any additional custom capabilities.
    /// </summary>
    public string? CapabilitiesVersion { get; set; }

    /// <summary>
    /// If <c>true</c> compiler will preserve intermediate compilation artifacts; otherwise they will be deleted.
    /// </summary>
    public bool PreserveBuildArtifacts { get; set; }
}