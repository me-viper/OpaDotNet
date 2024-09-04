using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore;

public interface IOpaImportsAbiFactory
{
    Func<IOpaImportsAbi> ImportsAbi { get; }

    Func<Stream?> Capabilities { get; }
}