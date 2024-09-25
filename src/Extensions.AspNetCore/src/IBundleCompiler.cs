using JetBrains.Annotations;

using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Extensions.AspNetCore;

[PublicAPI]
public interface IBundleCompiler
{
    IRegoCompiler Compiler { get; }

    RegoCompilerOptions CompilerOptions { get; }

    Task<Stream?> Compile(string source, CancellationToken cancellationToken);

    Task<Stream?> Compile(
        string source,
        Action<CompilationParameters> configureCompiler,
        CancellationToken cancellationToken);

    Task<Stream?> Compile(Stream source, CancellationToken cancellationToken);

    Task<Stream?> Compile(
        Stream source,
        Action<CompilationParameters> configureCompiler,
        CancellationToken cancellationToken
        );
}