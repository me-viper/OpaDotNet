using OpaDotNet.Compilation.Abstractions;
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

    protected IRegoCompiler MakeCompiler()
    {
        return new TestingCompiler(LoggerFactory);
    }

    // private IRegoCompiler Cli()
    // {
    //     return new RegoCliCompiler(
    //         new(),
    //         logger: LoggerFactory.CreateLogger<RegoCliCompiler>()
    //         );
    // }

    protected async Task<Stream> CompileBundle(string path, string[]? entrypoints = null, string? caps = null)
    {
        var cp = Options ?? new CompilationParameters();

        cp = cp with
        {
            Entrypoints = entrypoints,
            CapabilitiesFilePath = caps,
        };

        return await MakeCompiler().CompileBundleAsync(path, cp);
    }

    protected async Task<Stream> CompileBundle(string path, Stream caps, Func<CompilationParameters, CompilationParameters>? configure = null)
    {
        var cp = Options ?? new CompilationParameters();

        if (configure != null)
            cp = configure.Invoke(cp);

        Memory<byte> mem = new byte[caps.Length];
        _ = await caps.ReadAsync(mem);

        cp = cp with
        {
            CapabilitiesBytes = mem,
        };

        return await MakeCompiler().Compile(path, cp, CancellationToken.None);
    }

    protected async Task<Stream> CompileFile(string path, string[]? entrypoints = null)
    {
        var cp = Options ?? new CompilationParameters();

        cp = cp with
        {
            Entrypoints = entrypoints,
        };

        return await MakeCompiler().CompileFileAsync(path, cp);
    }

    protected async Task<Stream> CompileSource(string path, string[]? entrypoints = null)
    {
        var cp = Options ?? new CompilationParameters();

        cp = cp with
        {
            Entrypoints = entrypoints,
        };

        return await MakeCompiler().CompileSourceAsync(path, cp);
    }

    protected async Task<Stream> CompileBundle(Stream bundle, string[]? entrypoints = null)
    {
        var cp = Options ?? new CompilationParameters();

        cp = cp with
        {
            Entrypoints = entrypoints,
        };

        return await MakeCompiler().CompileBundleAsync(bundle, cp);
    }
}