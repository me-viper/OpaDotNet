using System.Text.Json;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Extensions.AspNetCore.Tests.Common;
using OpaDotNet.InternalTesting;
using OpaDotNet.Wasm;

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
        var tom = TestOptionsMonitor.Create(opts);

        return new CompiledBundlePolicySource(
            tom,
#pragma warning disable CA2000
            new MutableOpaEvaluatorFactory(),
#pragma warning restore CA2000
            LoggerFactory
            );
    }

    protected override async Task WritePolicy(string policy)
    {
        var compiler = new TestingCompiler();
        await using var bundle = await compiler.CompileSourceAsync(policy, new());

        await using var fs = new FileStream("./Watch/policy.tar.gz", FileMode.Create);
        await bundle.CopyToAsync(fs);
    }
}