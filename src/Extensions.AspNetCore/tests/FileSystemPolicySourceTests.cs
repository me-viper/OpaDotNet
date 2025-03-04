using System.Text.Json;

using OpaDotNet.Extensions.AspNetCore.Tests.Common;
using OpaDotNet.InternalTesting;
using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore.Tests;

[UsedImplicitly]
public sealed class FileSystemPolicySourceTests(ITestOutputHelper output) : PathPolicySourceTests<FileSystemPolicySource>(output)
{
    protected override FileSystemPolicySource CreatePolicySource(
        bool forceBundleWriter,
        Action<OpaAuthorizationOptions>? configure = null)
    {
        var opts = new OpaAuthorizationOptions
        {
            PolicyBundlePath = "./Watch",
            Compiler = new() { ForceBundleWriter = forceBundleWriter },
            EngineOptions = new WasmPolicyEngineOptions
            {
                SerializationOptions = new()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                },
            },
        };

        configure?.Invoke(opts);

        var authOptions = TestOptionsMonitor.Create(opts);
        var ric = new TestingCompiler(LoggerFactory);

        return new FileSystemPolicySource(
            new BundleCompiler(ric, authOptions, []),
            authOptions,
#pragma warning disable CA2000
            new MutableOpaEvaluatorFactory(),
#pragma warning restore CA2000
            LoggerFactory
            );
    }

    protected override async Task WritePolicy(string policy)
    {
        await File.WriteAllTextAsync("./Watch/policy.rego", policy);
    }
}