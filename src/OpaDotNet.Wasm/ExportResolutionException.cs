using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

namespace OpaDotNet.Wasm;

[PublicAPI]
[ExcludeFromCodeCoverage]
public class ExportResolutionException : OpaRuntimeException
{
    public Version AbiVersion { get; private set; }

    public string ExternalName { get; private set; }

    public ExportResolutionException(Version abiVersion, string externalName) : base(MakeMessage(abiVersion, externalName))
    {
        AbiVersion = abiVersion;
        ExternalName = externalName;
    }

    private static string MakeMessage(Version ver, string name) => $"ABI {ver}. Failed to resolve export {name}";
}