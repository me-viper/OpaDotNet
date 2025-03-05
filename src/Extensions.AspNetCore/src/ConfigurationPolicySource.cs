using Microsoft.Extensions.Options;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Extensions.AspNetCore.Telemetry;

namespace OpaDotNet.Extensions.AspNetCore;

[UsedImplicitly]
public class ConfigurationPolicySource : OpaPolicySource
{
    private readonly IDisposable? _policyChangeMonitor;

    private OpaPolicyOptions _opts;

    private readonly IBundleCompiler _compiler;

    public ConfigurationPolicySource(
        IBundleCompiler compiler,
        IOptionsMonitor<OpaAuthorizationOptions> authOptions,
        IOptionsMonitor<OpaPolicyOptions> policy,
        IMutableOpaEvaluatorFactory evaluatorFactory,
        ILoggerFactory loggerFactory) : base(authOptions, evaluatorFactory, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(compiler);
        ArgumentNullException.ThrowIfNull(policy);

        _compiler = compiler;

        _opts = policy.CurrentValue;
        _policyChangeMonitor = policy.OnChange(
            p =>
            {
                try
                {
                    if (!HasChanged(p, _opts))
                    {
                        Logger.BundleCompilationNoChanges();
                        return;
                    }

                    _opts = p;
                    Task.Run(() => CompileBundle(true)).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            );
    }

    private static bool HasChanged(OpaPolicyOptions a, OpaPolicyOptions b)
    {
        if (ReferenceEquals(a, b))
            return false;

        if (a.Keys.Count != b.Keys.Count)
            return true;

        foreach (var (k, v) in a)
        {
            if (!b.TryGetValue(k, out var ov))
                return true;

            if (!v.Equals(ov))
                return true;
        }

        return false;
    }

    /// <inheritdoc />
    protected override async Task<Stream?> CompileBundleFromSource(bool recompiling, CancellationToken cancellationToken = default)
    {
        var hasSources = false;
        using var ms = new MemoryStream();
        var bundleWriter = new BundleWriter(ms);

        await using (bundleWriter.ConfigureAwait(false))
        {
            foreach (var (name, policy) in _opts)
            {
                if (!string.IsNullOrWhiteSpace(policy.DataJson))
                    bundleWriter.WriteEntry(policy.DataJson, $"/{policy.Package}/data.json");

                if (!string.IsNullOrWhiteSpace(policy.DataYaml))
                    bundleWriter.WriteEntry(policy.DataYaml, $"/{policy.Package}/data.yaml");

                if (!string.IsNullOrWhiteSpace(policy.Source))
                {
                    hasSources = true;
                    bundleWriter.WriteEntry(policy.Source, $"/{policy.Package}/{name}.rego");
                }
            }
        }

        if (!hasSources)
            throw new RegoCompilationException("Configuration has no policies defined");

        ms.Seek(0, SeekOrigin.Begin);

        var result = await _compiler.Compile(
            ms,
            p => p.IsBundle = true,
            cancellationToken: cancellationToken
            ).ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing)
                _policyChangeMonitor?.Dispose();
        }
        finally
        {
            base.Dispose(disposing);
        }
    }
}