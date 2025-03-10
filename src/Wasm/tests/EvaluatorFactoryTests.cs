﻿using OpaDotNet.Wasm.Tests.Common;

namespace OpaDotNet.Wasm.Tests;

public class EvaluatorFactoryTests(ITestOutputHelper output) : OpaTestBase(output)
{
    [Fact]
    public async Task ParallelBundle()
    {
        await using var policyBundle = await CompileBundle(
            Path.Combine("TestData", "compile-bundle", "example"),
            ["test1/hello", "test2/hello"]
            );

        using var factory = new OpaBundleEvaluatorFactory(policyBundle);

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
        using var factory = new OpaWasmEvaluatorFactory(File.OpenRead(path));

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

        //await RunTest();
        await Parallel.ForEachAsync(Enumerable.Range(0, 100), async (_, _) => await RunTest());
    }

    [Fact]
    public Task StreamFileBundle()
    {
        var di = new DirectoryInfo("cache");

        if (di.Exists)
            di.Delete(true);

        di.Create();

        var path = Path.Combine("TestData", "compile-bundle", "bundle.tar.gz");

        var opts = new WasmPolicyEngineOptions
        {
            CachePath = di.FullName,
        };

        var factory = new OpaBundleEvaluatorFactory(File.OpenRead(path), opts);

        var evaluator1 = factory.Create();
        var input1 = new { message = "world" };
        var test1Result = evaluator1.EvaluatePredicate(input1, "test1/hello");

        Assert.True(test1Result.Result);

        var evaluator2 = factory.Create();
        var input2 = new { message = "world" };
        var test2Result = evaluator2.EvaluatePredicate(input2, "test1/hello");

        Assert.True(test2Result.Result);

        var cache = di.GetDirectories();
        Assert.Single(cache);

        var files = cache[0].GetFiles();

        Assert.Contains(files, p => string.Equals(p.Name, "bundle.tar.gz", StringComparison.Ordinal));

        factory.Dispose();

        Assert.False(Directory.Exists(cache[0].FullName));

        return Task.CompletedTask;
    }

    [Fact]
    public Task StreamFileWasm()
    {
        var di = new DirectoryInfo("cache");

        if (di.Exists)
            di.Delete(true);

        di.Create();

        var policyPath = Path.Combine("TestData", "compile-bundle", "policy.wasm");
        var dataPath = Path.Combine("TestData", "compile-bundle", "data.json");

        var opts = new WasmPolicyEngineOptions
        {
            CachePath = di.FullName,
        };

        var factory = new OpaWasmEvaluatorFactory(File.OpenRead(policyPath), opts);

        var evaluator1 = factory.Create();
        using var data1 = File.OpenRead(dataPath);
        evaluator1.SetDataFromStream(data1);
        var input1 = new { message = "world" };
        var test1Result = evaluator1.EvaluatePredicate(input1, "test1/hello");

        Assert.True(test1Result.Result);

        var evaluator2 = factory.Create();
        using var data2 = File.OpenRead(dataPath);
        evaluator2.SetDataFromStream(data2);
        var input2 = new { message = "world" };
        var test2Result = evaluator2.EvaluatePredicate(input2, "test1/hello");

        Assert.True(test2Result.Result);

        var cache = di.GetDirectories();
        Assert.Single(cache);

        var files = cache[0].GetFiles();

        Assert.Contains(files, p => string.Equals(p.Name, "policy.wasm", StringComparison.Ordinal));

        factory.Dispose();

        Assert.False(Directory.Exists(cache[0].FullName));

        return Task.CompletedTask;
    }
}