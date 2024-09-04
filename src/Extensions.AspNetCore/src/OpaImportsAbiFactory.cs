using Microsoft.Extensions.Options;

using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore;

internal class OpaImportsAbiFactory : IOpaImportsAbiFactory
{
    public Func<IOpaImportsAbi> ImportsAbi { get; }

    public Func<Stream?> Capabilities { get; } = () => null;

    internal OpaImportsAbiFactory()
    {
        ImportsAbi = () => new CoreImportsAbi();
    }

    public OpaImportsAbiFactory(Func<IOpaImportsAbi> importsAbi, IOptionsMonitor<OpaAuthorizationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(importsAbi);

        ImportsAbi = importsAbi;

        Capabilities = () =>
        {
            var path = options.CurrentValue.Compiler?.CapabilitiesFilePath;
            return !string.IsNullOrWhiteSpace(path) ? GetCapsFromFile(path) : null;
        };
    }

    public OpaImportsAbiFactory(
        Func<IOpaImportsAbi> importsAbi,
        Func<Stream> capabilities,
        IOptionsMonitor<OpaAuthorizationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(importsAbi);
        ArgumentNullException.ThrowIfNull(capabilities);

        ImportsAbi = importsAbi;

        Capabilities = () =>
        {
            var path = options.CurrentValue.Compiler?.CapabilitiesFilePath;
            return !string.IsNullOrWhiteSpace(path) ? GetCapsFromFile(path) : capabilities();
        };
    }

    private static FileStream GetCapsFromFile(string path) => new(path, FileMode.Open, FileAccess.Read);
}