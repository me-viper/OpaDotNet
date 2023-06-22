using JetBrains.Annotations;

namespace OpaDotNet.Wasm.Compilation;

[PublicAPI]
public interface IRegoCompiler
{
    Task<Stream> CompileBundle(
        string bundlePath,
        string[]? entrypoints = null,
        string? capabilitiesFilePath = null,
        CancellationToken cancellationToken = default);

    Task<Stream> CompileFile(
        string sourceFilePath,
        string[]? entrypoints = null,
        CancellationToken cancellationToken = default);
}