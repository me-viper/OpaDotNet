using Microsoft.Extensions.Options;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore;

internal sealed class BundleCompiler : IBundleCompiler
{
    private readonly Memory<byte> _capsCache = Memory<byte>.Empty;

    public IRegoCompiler Compiler { get; }

    public RegoCompilerOptions CompilerOptions { get; }

    public BundleCompiler(
        IRegoCompiler compiler,
        IOptionsMonitor<OpaAuthorizationOptions> compilerOptions,
        IEnumerable<ICapabilitiesProvider> capabilitiesProviders)
    {
        ArgumentNullException.ThrowIfNull(compiler);
        ArgumentNullException.ThrowIfNull(compilerOptions);
        ArgumentNullException.ThrowIfNull(capabilitiesProviders);

        CompilerOptions = compilerOptions.CurrentValue.Compiler ?? new();
        Compiler = compiler;

        var capsStream = Stream.Null;

        try
        {
            var capsPath = compilerOptions.CurrentValue.Compiler?.CapabilitiesFilePath;

            if (!string.IsNullOrWhiteSpace(capsPath))
            {
                capsStream = new FileStream(
                    capsPath,
                    FileMode.Open,
                    FileAccess.Read
                    );
            }
            else
            {
                var caps = capabilitiesProviders.ToList();

                if (caps.Count >= 1)
                {
                    capsStream = caps[0].GetCapabilities();

                    foreach (var cap in caps[1..])
                    {
                        using var cs = cap.GetCapabilities();
                        using var oldCaps = capsStream;
                        capsStream = BundleWriter.MergeCapabilities(oldCaps, cs);
                    }
                }

                capsStream.Seek(0, SeekOrigin.Begin);
            }

            if (capsStream.Length > 0)
            {
                _capsCache = new byte[capsStream.Length];
                _ = capsStream.Read(_capsCache.Span);
            }
        }
        finally
        {
            capsStream.Dispose();
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

    public Task<Stream?> Compile(string source, CancellationToken cancellationToken) => Compile(source, _ => { }, cancellationToken);

    public async Task<Stream?> Compile(
        string source,
        Action<CompilationParameters> configureCompiler,
        CancellationToken cancellationToken)
    {
        var cp = MakeParameters();
        configureCompiler(cp);
        return await Compiler.Compile(source, cp, cancellationToken).ConfigureAwait(false);
    }

    public Task<Stream?> Compile(Stream source, CancellationToken cancellationToken) => Compile(source, _ => { }, cancellationToken);

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