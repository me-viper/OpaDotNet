using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class EvaluatorFactoryTests : OpaTestBase, IAsyncLifetime
{
    private Stream _policyBundle = default!;

    public EvaluatorFactoryTests(ITestOutputHelper output) : base(output)
    {
    }

    public async Task InitializeAsync()
    {
        _policyBundle = await CompileBundle(
            Path.Combine("TestData", "compile-bundle", "example"),
            new[] { "test1/hello", "test2/hello" }
            );
    }

    [Fact]
    public async Task ParallelBundle()
    {
        var factory = new OpaBundleEvaluatorFactory(
            _policyBundle,
            loggerFactory: LoggerFactory
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
            loggerFactory: LoggerFactory
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