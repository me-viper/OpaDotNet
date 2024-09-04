using System.Text.Json;

using JetBrains.Annotations;

using Microsoft.Extensions.Options;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Interop;
using OpaDotNet.Extensions.AspNetCore.Tests.Common;
using OpaDotNet.Wasm;

using Xunit.Abstractions;

namespace OpaDotNet.Extensions.AspNetCore.Tests;

[UsedImplicitly]
public class CompiledBundlePolicySourceTests(ITestOutputHelper output) : PathPolicySourceTests<CompiledBundlePolicySource>(output)
{
    protected override CompiledBundlePolicySource CreatePolicySource(
        bool forceBundleWriter,
        Action<OpaAuthorizationOptions>? configure = null)
    {
        var opts = new OpaAuthorizationOptions
        {
            PolicyBundlePath = "./Watch/policy.tar.gz",
            EngineOptions = new WasmPolicyEngineOptions
            {
                SerializationOptions = new()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                },
            },
        };

        configure?.Invoke(opts);

        return new CompiledBundlePolicySource(
            TestOptionsMonitor.Create(opts),
            new OpaImportsAbiFactory(),
            LoggerFactory
            );
    }

    protected override async Task WritePolicy(string policy)
    {
        var compiler = new RegoInteropCompiler();
        await using var bundle = await compiler.CompileSourceAsync(policy, new());

        await using var fs = new FileStream("./Watch/policy.tar.gz", FileMode.Create);
        await bundle.CopyToAsync(fs);
    }
}