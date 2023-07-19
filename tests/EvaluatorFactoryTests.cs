using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class EvaluatorFactoryTests : IAsyncLifetime
{
    private readonly ILoggerFactory _loggerFactory;

    private Stream _policyBundle = default!;

    public EvaluatorFactoryTests(ITestOutputHelper output)
    {
        _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
    }

    public async Task InitializeAsync()
    {
        var compiler = new RegoCliCompiler(
            logger: _loggerFactory.CreateLogger<RegoCliCompiler>()
            );

        _policyBundle = await compiler.CompileBundle(
            Path.Combine("TestData", "compile-bundle", "example"),
            new[] { "test1/hello", "test2/hello" }
            );
    }

    [Fact]
    public async Task ParallelBundle()
    {
        var factory = new OpaBundleEvaluatorFactory(
            _policyBundle,
            loggerFactory: _loggerFactory
            );

        Task RunTest()
        {
            var evaluator = factory.Create();

            var input1 = new { message = "world" };
            var test1Result = evaluator.EvaluatePredicate(input1, "test1/hello");

            Assert.True(test1Result.Result);

            var input2 = new { message = "world1" };
            var test2Result = evaluator.EvaluatePredicate(input2, "test2/hello");

            Assert.True(test2Result.Result);
            return Task.CompletedTask;
        }

        await Parallel.ForEachAsync(Enumerable.Range(0, 100), async (_, _) => await RunTest());
    }

    [Fact]
    public async Task ParallelWasm()
    {
        var path = Path.Combine("TestData", "compile-bundle", "policy.wasm");
        var factory = new OpaWasmEvaluatorFactory(
            File.OpenRead(path),
            loggerFactory: _loggerFactory
            );

        using var dataStream = File.OpenText(Path.Combine("TestData", "compile-bundle", "data.json"));
        var data = await dataStream.ReadToEndAsync();

        Task RunTest()
        {
            var evaluator = factory.Create();
            evaluator.SetDataFromRawJson(data);

            var input1 = new { message = "world" };
            var test1Result = evaluator.EvaluatePredicate(input1, "test1/hello");

            Assert.True(test1Result.Result);

            return Task.CompletedTask;
        }

        await Parallel.ForEachAsync(Enumerable.Range(0, 100), async (_, _) => await RunTest());
    }

    public Task DisposeAsync()
    {
        _policyBundle.DisposeAsync();

        return Task.CompletedTask;
    }
}