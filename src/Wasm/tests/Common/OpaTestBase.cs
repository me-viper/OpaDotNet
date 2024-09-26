using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;
using OpaDotNet.Compilation.Interop;
using OpaDotNet.InternalTesting;

namespace OpaDotNet.Wasm.Tests.Common;

public class OpaTestBase
{
    protected ITestOutputHelper Output { get; }

    protected ILoggerFactory LoggerFactory { get; }

    protected CompilationParameters? Options { get; init; }

    protected OpaTestBase(ITestOutputHelper output)
    {
        Output = output;
        LoggerFactory = new LoggerFactory([new XunitLoggerProvider(output)]);
    }

    private IRegoCompiler Interop()
    {
        return new RegoInteropCompiler(
            logger: LoggerFactory.CreateLogger<RegoInteropCompiler>()
            );
    }

    private IRegoCompiler Cli()
    {
        return new RegoCliCompiler(
            new(),
            logger: LoggerFactory.CreateLogger<RegoCliCompiler>()
            );
    }

    protected async Task<Stream> CompileBundle(string path, string[]? entrypoints = null, string? caps = null)
    {
        var cp = Options ?? new CompilationParameters();

        cp = cp with
        {
            Entrypoints = entrypoints,
            CapabilitiesFilePath = caps,
        };

        return await Interop().CompileBundleAsync(path, cp);
    }

    protected async Task<Stream> CompileBundle(string path, string[]? entrypoints, Stream caps)
    {
        var cp = Options ?? new CompilationParameters();

        Memory<byte> mem = new byte[caps.Length];
        _ = await caps.ReadAsync(mem);

        cp = cp with
        {
            Entrypoints = entrypoints,
            CapabilitiesBytes = mem,
        };

        return await Interop().Compile(path, cp, CancellationToken.None);
    }

    protected async Task<Stream> CompileFile(string path, string[]? entrypoints = null)
    {
        var cp = Options ?? new CompilationParameters();

        cp = cp with
        {
            Entrypoints = entrypoints,
        };

        return await Interop().CompileFileAsync(path, cp);
    }

    protected async Task<Stream> CompileSource(string path, string[]? entrypoints = null)
    {
        var cp = Options ?? new CompilationParameters();

        cp = cp with
        {
            Entrypoints = entrypoints,
        };

        return await Interop().CompileSourceAsync(path, cp);
    }
}