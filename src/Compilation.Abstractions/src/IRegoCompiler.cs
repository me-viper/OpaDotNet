namespace OpaDotNet.Compilation.Abstractions;

/// <summary>
/// Exposes an OPA policy compiler.
/// </summary>
[PublicAPI]
public interface IRegoCompiler
{
    /// <summary>
    /// Compiler version information.
    /// </summary>
    Task<RegoCompilerVersion> Version(CancellationToken cancellationToken = default);

    /// <summary>
    /// Compiles OPA bundle from path.
    /// </summary>
    /// <param name="path">Source path.</param>
    /// <param name="parameters">Compilation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    Task<Stream> Compile(string path, CompilationParameters parameters, CancellationToken cancellationToken);

    /// <summary>
    /// Compiles OPA bundle from stream.
    /// </summary>
    /// <param name="stream">Source stream.</param>
    /// <param name="parameters">Compilation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    Task<Stream> Compile(Stream stream, CompilationParameters parameters, CancellationToken cancellationToken);
}