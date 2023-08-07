using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

namespace OpaDotNet.Wasm;

/// <summary>
/// The exception that is thrown when specified export does not exist in WASM module.
/// </summary>
[PublicAPI]
[ExcludeFromCodeCoverage]
public class ExportResolutionException : OpaRuntimeException
{
    /// <summary>
    /// OPA WASM module ABI version.
    /// </summary>
    public Version AbiVersion { get; private set; }

    /// <summary>
    /// External that caused the exception.
    /// </summary>
    public string ExternalName { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportResolutionException"/>.
    /// </summary>
    /// <param name="abiVersion">OPA WASM module ABI version.</param>
    /// <param name="externalName">External that caused the exception.</param>
    public ExportResolutionException(Version abiVersion, string externalName) : base(MakeMessage(abiVersion, externalName))
    {
        AbiVersion = abiVersion;
        ExternalName = externalName;
    }

    private static string MakeMessage(Version ver, string name) => $"ABI {ver}. Failed to resolve export {name}";
}