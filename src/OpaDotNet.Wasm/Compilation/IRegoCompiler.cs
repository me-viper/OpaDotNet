using JetBrains.Annotations;

namespace OpaDotNet.Wasm.Compilation;

[PublicAPI]
public interface IRegoCompiler
{
    /// <summary>
    /// Compiles OPA bundle from bundle directory. 
    /// </summary>
    /// <param name="bundlePath">Bundle directory path</param>
    /// <param name="entrypoints">Entrypoints</param>
    /// <param name="capabilitiesFilePath"></param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compiled OPA bundle stream</returns>
    Task<Stream> CompileBundle(
        string bundlePath,
        IEnumerable<string>? entrypoints = null,
        string? capabilitiesFilePath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compiles OPA bundle from rego policy source file.
    /// </summary>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="entrypoints">Entrypoints</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compiled OPA bundle stream</returns>
    Task<Stream> CompileFile(
        string sourceFilePath,
        IEnumerable<string>? entrypoints = null,
        CancellationToken cancellationToken = default);
}