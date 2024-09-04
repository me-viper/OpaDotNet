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
public sealed class FileSystemPolicySourceTests : PathPolicySourceTests<FileSystemPolicySource>
{
    public FileSystemPolicySourceTests(ITestOutputHelper output) : base(output)
    {
    }

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
        var imports = new OpaImportsAbiFactory();
        var ric = new RegoInteropCompiler();

        return new FileSystemPolicySource(
            new BundleCompiler(ric, authOptions, imports),
            authOptions,
            imports,
            LoggerFactory
            );
    }

    protected override async Task WritePolicy(string policy)
    {
        await File.WriteAllTextAsync("./Watch/policy.rego", policy);
    }
}