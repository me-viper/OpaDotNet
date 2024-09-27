using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;
using OpaDotNet.Compilation.Interop;

namespace OpaDotNet.InternalTesting;

public class TestingCompiler : IRegoCompiler
{
    private readonly IRegoCompiler _inner;

    public TestingCompiler() : this(NullLoggerFactory.Instance)
    {
    }

    public TestingCompiler(ILoggerFactory loggerFactory)
    {
        var compiler = Environment.GetEnvironmentVariable("OPA_TEST_COMPILER");

        if (string.Equals(compiler, "cli", StringComparison.OrdinalIgnoreCase))
            _inner = new RegoCliCompiler(logger: loggerFactory.CreateLogger<RegoCliCompiler>());
        else
            _inner = new RegoInteropCompiler(loggerFactory.CreateLogger<RegoInteropCompiler>());
    }

    public Task<RegoCompilerVersion> Version(CancellationToken cancellationToken = default) => _inner.Version(cancellationToken);

    public Task<Stream> Compile(string path, CompilationParameters parameters, CancellationToken cancellationToken)
        => _inner.Compile(path, parameters, cancellationToken);

    public Task<Stream> Compile(Stream stream, CompilationParameters parameters, CancellationToken cancellationToken)
        => _inner.Compile(stream, parameters, cancellationToken);
}