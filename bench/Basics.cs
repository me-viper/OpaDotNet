using System.Text.Json.Serialization;

using BenchmarkDotNet.Attributes;

using OpaDotNet.Wasm;

namespace OpaDotNet.Benchmarks;

[MemoryDiagnoser]
public class Basics
{
    private IOpaEvaluator _engine = default!;

    private record Input
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = default!;
    }

    private record Data
    {
        [JsonPropertyName("world")]
        public string World { get; set; } = default!;
    }

    private static Input InputInstance { get; } = new() { Message = "world" };

    private static Data DataInstance { get; } = new() { World = "world" };

    [GlobalSetup]
    public void Setup()
    {
        var policy = File.OpenRead(Path.Combine("Data", "simple-1.3.wasm"));
        _engine = OpaEvaluatorFactory.CreateFromWasm(policy, new() { MaxMemoryPages = 3 });
        _engine.SetData(DataInstance);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _engine.Dispose();
    }

    [Benchmark(Baseline = true)]
    public bool Native()
    {
        var result = InputInstance.Message == DataInstance.World;

        if (!result)
            throw new Exception("Unexpected result");

        return result;
    }

    [Benchmark]
    public bool Wasm()
    {
        var r = _engine.EvaluatePredicate(InputInstance);
        var result = r.Result;

        if (!result)
            throw new Exception("Unexpected result");

        return result;
    }
}