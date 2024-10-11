using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Extensions.AspNetCore;

internal sealed class NopCompiler : IRegoCompiler
{
    public static IRegoCompiler Instance { get; } = new NopCompiler();

    public Task<RegoCompilerVersion> Version(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("No valid compiler configured");
    }

    public Task<Stream> Compile(string path, CompilationParameters parameters, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("No valid compiler configured");
    }

    public Task<Stream> Compile(Stream stream, CompilationParameters parameters, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("No valid compiler configured");
    }
}