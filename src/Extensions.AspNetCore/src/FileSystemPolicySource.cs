using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Extensions.AspNetCore.Telemetry;

namespace OpaDotNet.Extensions.AspNetCore;

[PublicAPI]
public sealed class FileSystemPolicySource : PathPolicySource
{
    private readonly IBundleCompiler _compiler;

    private readonly PhysicalFileProvider? _fileProvider;

    public FileSystemPolicySource(
        IBundleCompiler compiler,
        IOptionsMonitor<OpaAuthorizationOptions> options,
        IMutableOpaEvaluatorFactory evaluatorFactory,
        ILoggerFactory loggerFactory) : base(options, evaluatorFactory, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(compiler);

        _compiler = compiler;

        var path = Options.PolicyBundlePath!;

        if (!Path.IsPathRooted(Options.PolicyBundlePath!))
            path = Path.GetFullPath(Options.PolicyBundlePath!);

        if (MonitoringEnabled)
        {
            _fileProvider = new PhysicalFileProvider(
                path,
                ExclusionFilters.Sensitive
                );

            CompositeChangeToken MakePolicyChangeToken() => new(
                [
                    _fileProvider.Watch("**/*.rego"),
                    _fileProvider.Watch("**/data.json"),
                    _fileProvider.Watch("**/data.yaml"),
                ]
                );

            void OnPolicyChange()
            {
                Logger.BundleCompilationHasChanges();
                NeedsRecompilation = true;
            }

            PolicyWatcher = ChangeToken.OnChange(MakePolicyChangeToken, OnPolicyChange);
        }
    }

    protected override async Task<Stream?> CompileBundleFromSource(bool recompiling, CancellationToken cancellationToken = default)
    {
        if (!_compiler.CompilerOptions.ForceBundleWriter)
        {
            return await _compiler.Compile(
                Options.PolicyBundlePath!,
                p => p.IsBundle = true,
                cancellationToken: cancellationToken
                ).ConfigureAwait(false);
        }

        using var ms = new MemoryStream();

        var bundle = BundleWriter.FromDirectory(
            ms,
            Options.PolicyBundlePath!,
            _compiler.CompilerOptions.Ignore
            );

        await bundle.DisposeAsync().ConfigureAwait(false);
        ms.Seek(0, SeekOrigin.Begin);

        return await _compiler.Compile(ms, p => p.IsBundle = true, cancellationToken).ConfigureAwait(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _fileProvider?.Dispose();

        base.Dispose(disposing);
    }
}