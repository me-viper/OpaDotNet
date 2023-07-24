using JetBrains.Annotations;

using Microsoft.Extensions.Options;

using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class MetadataTests : IAsyncLifetime
{
    private IOpaEvaluator _engine = default!;

    private readonly ILoggerFactory _loggerFactory;

    private readonly OptionsWrapper<RegoCliCompilerOptions> _options = new(new());

    private string BasePath { get; } = Path.Combine("TestData", "metadata");

    public MetadataTests(ITestOutputHelper output)
    {
        _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
    }

    public async Task InitializeAsync()
    {
        var compiler = new RegoCliCompiler(_options, _loggerFactory.CreateLogger<RegoCliCompiler>());
        var policy = await compiler.CompileBundle(
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

        _engine = OpaEvaluatorFactory.CreateFromBundle(policy, opts, loggerFactory: _loggerFactory);
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