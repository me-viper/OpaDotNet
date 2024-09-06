using JetBrains.Annotations;

using OpaDotNet.Wasm.Tests.Common;

using Xunit.Abstractions;

namespace OpaDotNet.Wasm.Tests;

public class MetadataTests : OpaTestBase, IAsyncLifetime
{
    private IOpaEvaluator _engine = default!;

    private string BasePath { get; } = Path.Combine("TestData", "metadata");

    public MetadataTests(ITestOutputHelper output) : base(output)
    {
    }

    public async Task InitializeAsync()
    {
        var policy = await CompileBundle(
            BasePath,
            new[] { "example" }
            );

        var opts = new WasmPolicyEngineOptions
        {
            SerializationOptions = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            },
        };

        _engine = OpaEvaluatorFactory.CreateFromBundle(policy, opts);
    }

    public Task DisposeAsync()
    {
        _engine.Dispose();
        return Task.CompletedTask;
    }

    private record MetaInput([UsedImplicitly] int Number, [UsedImplicitly] string Role);

    private class MetaOutput
    {
        public class DenyReason
        {
            [UsedImplicitly]
            public string? Severity { get; set; }

            [UsedImplicitly]
            public string? Reason { get; set; }
        }

        [UsedImplicitly]
        public List<DenyReason> Deny { get; set; } = new();
    }

    [Fact]
    public void MetadataRule()
    {
        var result = _engine.Evaluate<MetaInput, MetaOutput>(new(10, "xxx"), "example");

        Assert.Collection(
            result.Result.Deny,
            p => Assert.Equal("Numbers may not be higher than 5", p.Reason),
            p => Assert.Equal("Subject must have the 'admin' role", p.Reason)
            );
    }
}