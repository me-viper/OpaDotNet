﻿using JetBrains.Annotations;

namespace OpaDotNet.Wasm.Compilation;

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
    /// Directory should exist and requires write permissions.
    /// </remarks>
    public string? OutputPath { get; set; }

    /// <summary>
    /// Extra arguments to pass to opa cli tool.
    /// </summary>
    public string? ExtraArguments { get; set; }

    /// <summary>
    /// OPA capabilities version.
    /// </summary>
    public string? CapabilitiesVersion { get; set; }

    /// <summary>
    /// If <c>true</c> compiler will preserve intermediate compilation artifacts; otherwise they will be deleted.
    /// </summary>
    public bool PreserveBuildArtifacts { get; set; }
}