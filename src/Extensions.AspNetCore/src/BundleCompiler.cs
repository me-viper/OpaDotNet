using Microsoft.Extensions.Options;

using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Extensions.AspNetCore;

internal sealed class BundleCompiler : IBundleCompiler
{
    private readonly Memory<byte> _capsCache = Memory<byte>.Empty;

    public IRegoCompiler Compiler { get; }

    public RegoCompilerOptions CompilerOptions { get; }

    public BundleCompiler(
        IRegoCompiler compiler,
        IOptionsMonitor<OpaAuthorizationOptions> compilerOptions,
        IOpaImportsAbiFactory importsAbiFactory)
    {
        ArgumentNullException.ThrowIfNull(compiler);
        ArgumentNullException.ThrowIfNull(compilerOptions);
        ArgumentNullException.ThrowIfNull(importsAbiFactory);

        CompilerOptions = compilerOptions.CurrentValue.Compiler ?? new();
        Compiler = compiler;

        var capsStream = importsAbiFactory.Capabilities();

        if (capsStream != null)
        {
            _capsCache = new byte[capsStream.Length];
            _ = capsStream.Read(_capsCache.Span);
        }
    }

    private CompilationParameters MakeParameters()
    {
        var result = new CompilationParameters
        {
            Entrypoints = CompilerOptions.Entrypoints,
            Debug = CompilerOptions.Debug,
            Ignore = CompilerOptions.Ignore,
            CapabilitiesVersion = CompilerOptions.CapabilitiesVersion,
            FollowSymlinks = CompilerOptions.FollowSymlinks,
            PruneUnused = CompilerOptions.PruneUnused,
            OutputPath = CompilerOptions.OutputPath,
            RegoVersion = CompilerOptions.RegoVersion,
            DisablePrintStatements = CompilerOptions.DisablePrintStatements,
            CapabilitiesBytes = _capsCache,
        };

        return result;
    }

    public Task<Stream?> Compile(string source, CancellationToken cancellationToken) => Compile(source, _ => {}, cancellationToken);

    public async Task<Stream?> Compile(
        string source,
        Action<CompilationParameters> configureCompiler,
        CancellationToken cancellationToken)
    {
        var cp = MakeParameters();
        configureCompiler(cp);
        return await Compiler.Compile(source, cp, cancellationToken).ConfigureAwait(false);
    }

    public Task<Stream?> Compile(Stream source, CancellationToken cancellationToken) => Compile(source, _ => {}, cancellationToken);

    public async Task<Stream?> Compile(
        Stream source,
        Action<CompilationParameters> configureCompiler,
        CancellationToken cancellationToken)
    {
        var cp = MakeParameters();
        configureCompiler(cp);
        return await Compiler.Compile(source, cp, cancellationToken).ConfigureAwait(false);
    }
}