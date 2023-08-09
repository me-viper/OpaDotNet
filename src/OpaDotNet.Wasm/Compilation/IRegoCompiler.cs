using JetBrains.Annotations;

namespace OpaDotNet.Wasm.Compilation;

/// <summary>
/// Exposes an OPA policy compiler.
/// </summary>
[PublicAPI]
public interface IRegoCompiler
{
    /// <summary>
    /// Compiles OPA bundle from bundle directory.
    /// </summary>
    /// <param name="bundlePath">Bundle directory path.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="capabilitiesFilePath">
    /// Capabilities file that defines the built-in functions and other language features that policies may depend on.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    Task<Stream> CompileBundle(
        string bundlePath,
        IEnumerable<string>? entrypoints = null,
        string? capabilitiesFilePath = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compiles OPA bundle from rego policy source file.
    /// </summary>
    /// <param name="sourceFilePath">Source file path.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    Task<Stream> CompileFile(
        string sourceFilePath,
        IEnumerable<string>? entrypoints = null,
        CancellationToken cancellationToken = default);
}