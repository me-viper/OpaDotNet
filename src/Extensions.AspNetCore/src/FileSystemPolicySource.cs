using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Extensions.AspNetCore.Telemetry;

namespace OpaDotNet.Extensions.AspNetCore;

public sealed class FileSystemPolicySource : PathPolicySource
{
    private readonly IBundleCompiler _compiler;

    public FileSystemPolicySource(
        IBundleCompiler compiler,
        IOptionsMonitor<OpaAuthorizationOptions> options,
        IOpaBundleEvaluatorFactoryBuilder factoryBuilder,
        ILoggerFactory loggerFactory) : base(options, factoryBuilder, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(compiler);

        _compiler = compiler;

        var path = Options.PolicyBundlePath!;

        if (!Path.IsPathRooted(Options.PolicyBundlePath!))
            path = Path.GetFullPath(Options.PolicyBundlePath!);

        if (MonitoringEnabled)
        {
            var fileProvider = new PhysicalFileProvider(
                path,
                ExclusionFilters.Sensitive
                );

            CompositeChangeToken MakePolicyChangeToken() => new(
                [
                    fileProvider.Watch("**/*.rego"),
                    fileProvider.Watch("**/data.json"),
                    fileProvider.Watch("**/data.yaml"),
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
}