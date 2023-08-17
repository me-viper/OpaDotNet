using Microsoft.Extensions.Options;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;
using OpaDotNet.Compilation.Interop;

using Xunit.Abstractions;

namespace OpaDotNet.Tests.Common;

public class OpaTestBase
{
    protected ITestOutputHelper Output { get; }

    protected ILoggerFactory LoggerFactory { get; }

    protected RegoCompilerOptions? Options { get; set; }

    private IOptions<RegoCompilerOptions>? GetOptions() => Options == null
        ? null
        : new OptionsWrapper<RegoCompilerOptions>(Options);

    protected OpaTestBase(ITestOutputHelper output)
    {
        Output = output;
        LoggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
    }

    private IRegoCompiler Interop()
    {
        return new RegoInteropCompiler(
            GetOptions(),
            logger: LoggerFactory.CreateLogger<RegoInteropCompiler>()
            );
    }

    private IRegoCompiler Cli()
    {
        return new RegoCliCompiler(
            new OptionsWrapper<RegoCliCompilerOptions>(new() { Debug = false }),
            logger: LoggerFactory.CreateLogger<RegoCliCompiler>()
            );
    }

    protected async Task<Stream> CompileBundle(string path, string[]? entrypoints = null, string? caps = null)
    {
        return await Interop().CompileBundle(path, entrypoints, caps);
    }

    protected async Task<Stream> CompileFile(string path, string[]? entrypoints = null)
    {
        var compiler = new RegoInteropCompiler(
            GetOptions(),
            logger: LoggerFactory.CreateLogger<RegoInteropCompiler>()
            );

        return await Interop().CompileFile(path, entrypoints);
    }

    protected async Task<Stream> CompileSource(string path, string[]? entrypoints = null)
    {
        var compiler = new RegoInteropCompiler(
            GetOptions(),
            logger: LoggerFactory.CreateLogger<RegoInteropCompiler>()
            );

        return await Interop().CompileSource(path, entrypoints);
    }
}