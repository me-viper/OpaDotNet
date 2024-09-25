using JetBrains.Annotations;

namespace OpaDotNet.Compilation.Cli;

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
    /// Extra arguments to pass to <c>opa build</c> cli tool.
    /// </summary>
    public string? ExtraArguments { get; set; }

    /// <summary>
    /// If <c>true</c> compiler will preserve intermediate compilation artifacts; otherwise they will be deleted.
    /// </summary>
    public bool PreserveBuildArtifacts { get; set; }
}