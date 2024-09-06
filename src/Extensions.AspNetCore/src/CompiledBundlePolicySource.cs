using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Extensions.AspNetCore.Telemetry;

namespace OpaDotNet.Extensions.AspNetCore;

public sealed class CompiledBundlePolicySource : PathPolicySource
{
    public CompiledBundlePolicySource(
        IOptionsMonitor<OpaAuthorizationOptions> options,
        IOpaBundleEvaluatorFactoryBuilder factoryBuilder,
        ILoggerFactory loggerFactory) : base(options, factoryBuilder, loggerFactory)
    {
        var path = Options.PolicyBundlePath!;

        if (!Path.IsPathRooted(Options.PolicyBundlePath!))
            path = Path.GetFullPath(Options.PolicyBundlePath!);

        if (!File.Exists(path))
            throw new FileNotFoundException("Policy bundle file was not found", path);

        if (MonitoringEnabled)
        {
            var fileProvider = new PhysicalFileProvider(
                Path.GetDirectoryName(path)!,
                ExclusionFilters.Sensitive
                );

            var file = Path.GetFileName(path);

            CompositeChangeToken MakePolicyChangeToken() => new(new[] { fileProvider.Watch(file), });

            void OnPolicyChange()
            {
                Logger.BundleCompilationHasChanges();
                NeedsRecompilation = true;
            }

            PolicyWatcher = ChangeToken.OnChange(MakePolicyChangeToken, OnPolicyChange);
        }
    }

    protected override Task<Stream?> CompileBundleFromSource(bool recompiling, CancellationToken cancellationToken = default)
    {
        var stream = new FileStream(Options.PolicyBundlePath!, FileMode.Open, FileAccess.Read);
        return Task.FromResult<Stream?>(stream);
    }
}