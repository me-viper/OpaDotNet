using OpaDotNet.Wasm.Tests.Common;

using Wasmtime;

using Xunit.Abstractions;

namespace OpaDotNet.Wasm.Tests;

public class MemoryTests : OpaTestBase, IAsyncLifetime
{
    private Func<WasmPolicyEngineOptions, IOpaEvaluator> _engine = default!;

    private string BasePath { get; } = Path.Combine("TestData", "memory");

    public MemoryTests(ITestOutputHelper output) : base(output)
    {
    }

    public async Task InitializeAsync()
    {
        var policy = await CompileBundle(
            BasePath,
            new[] { "test/allow" }
            );

        _engine = p => OpaEvaluatorFactory.CreateFromBundle(policy, options: p, loggerFactory: LoggerFactory);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public Task HostFailsToGrow()
    {
        using var engine = _engine(new() { MaxMemoryPages = 3 });
        var input = new string('a', 2 * 65536);

        var ex = Assert.Throws<OpaEvaluationException>(() => engine.EvaluatePredicate(input));
        Assert.IsType<WasmtimeException>(ex.InnerException);
        Assert.StartsWith("failed to grow memory", ex.InnerException.Message);
        return Task.CompletedTask;
    }

    [Fact]
    public Task HostSucceedsToGrow()
    {
        using var engine = _engine(new() { MaxMemoryPages = 8 });
        var input = new string('a', 2 * 65536);

        engine.EvaluatePredicate(input);
        return Task.CompletedTask;
    }

    [Fact]
    public Task InputTooLarge()
    {
        using var engine = _engine(new() { MinMemoryPages = 3, MaxMemoryPages = 4 });
        var input = new string('a', 2 * 65536);

        Assert.Throws<OpaEvaluationException>(() => engine.EvaluatePredicate(input));
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotLeak()
    {
        using var engine = _engine(new() { MaxMemoryPages = 8 });
        var input = new string('a', 2 * 65536);

        for (var i = 0; i < 100; i++)
        {
            Output.WriteLine($"Iteration {i}");
            var r = engine.EvaluatePredicate(input);
            Assert.False(r.Result);
        }

        return Task.CompletedTask;
    }
}