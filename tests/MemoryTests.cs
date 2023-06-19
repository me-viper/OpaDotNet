using Microsoft.Extensions.Options;

using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

using Wasmtime;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class MemoryTests : IAsyncLifetime
{
    private Func<WasmPolicyEngineOptions, IOpaEvaluator> _engine = default!;
    
    private readonly ILoggerFactory _loggerFactory;
    
    private readonly ITestOutputHelper _output;
    
    private string BasePath { get; } = Path.Combine("TestData", "Opa", "memory");

    public MemoryTests(ITestOutputHelper output)
    {
        _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
        _output = output;
    }
    
    public async Task InitializeAsync()
    {
        var options = new OptionsWrapper<RegoCliCompilerOptions>(new());
        var compiler = new RegoCliCompiler(options, _loggerFactory.CreateLogger<RegoCliCompiler>());
        var policyStream = await compiler.CompileBundle(
            BasePath, 
            new[] { "test/allow" }
            );
        
        var factory = new OpaEvaluatorFactory(loggerFactory: _loggerFactory);
        _engine = p => factory.CreateWithJsonData(policyStream, options: p);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
    
    [Fact]
    public void HostFailsToGrow()
    {
        using var engine = _engine(new() { MaxMemoryPages = 3 });
        var input = new string('a', 2 * 65536);
        
        var ex = Assert.Throws<OpaEvaluationException>(() => engine.EvaluatePredicate(input));
        Assert.IsType<WasmtimeException>(ex.InnerException);
        Assert.StartsWith("failed to grow memory", ex.InnerException.Message);
    }
    
    [Fact]
    public void HostSucceedsToGrow()
    {
        using var engine = _engine(new() { MaxMemoryPages = 8 });
        var input = new string('a', 2 * 65536);
        
        engine.EvaluatePredicate(input);
    }
    
    [Fact]
    public void InputTooLarge()
    {
        using var engine = _engine(new() { MinMemoryPages = 3, MaxMemoryPages = 4 });
        var input = new string('a', 2 * 65536);
        
        Assert.Throws<OpaEvaluationException>(() => engine.EvaluatePredicate(input));
    }
    
    [Fact]
    public void DoesNotLeak()
    {
        using var engine = _engine(new() { MaxMemoryPages = 8 });
        var input = new string('a', 2 * 65536);
        
        for (var i = 0; i < 100; i++)
        {
            _output.WriteLine($"Iteration {i}");
            var r = engine.EvaluatePredicate(input);
            Assert.False(r.Result);
        }
    }
}