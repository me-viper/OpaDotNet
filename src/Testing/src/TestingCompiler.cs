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

    public static RegoCliCompiler CreateCliCompiler(ILoggerFactory? loggerFactory = null, RegoCliCompilerOptions? options = null)
    {
        var opts = options ?? new RegoCliCompilerOptions();
        var logger = loggerFactory?.CreateLogger<RegoCliCompiler>();

        var cliPath = Environment.GetEnvironmentVariable("OPA_TEST_COMPILER_CLI_PATH");

        if (!string.IsNullOrWhiteSpace(cliPath))
            opts.OpaToolPath = cliPath;

        return new RegoCliCompiler(logger, opts);
    }

    public TestingCompiler(ILoggerFactory loggerFactory)
    {
        var compiler = Environment.GetEnvironmentVariable("OPA_TEST_COMPILER");

        if (string.Equals(compiler, "cli", StringComparison.OrdinalIgnoreCase))
        {
            _inner = CreateCliCompiler(loggerFactory);
            return;
        }

        _inner = new RegoInteropCompiler(loggerFactory.CreateLogger<RegoInteropCompiler>());
    }

    public Task<RegoCompilerVersion> Version(CancellationToken cancellationToken = default) => _inner.Version(cancellationToken);

    public Task<Stream> Compile(string path, CompilationParameters parameters, CancellationToken cancellationToken)
        => _inner.Compile(path, parameters, cancellationToken);

    public Task<Stream> Compile(Stream stream, CompilationParameters parameters, CancellationToken cancellationToken)
        => _inner.Compile(stream, parameters, cancellationToken);
}