using System.Text.Json.Serialization;

using BenchmarkDotNet.Attributes;

using OpaDotNet.Wasm;

namespace OpaDotNet.Benchmarks;

[Config(typeof(Config))]
public class Factory
{
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

    [Params(10)]

    //[Params(10, 100)]
    public int Iterations { get; set; }

    [Benchmark]
    public bool[] InMemory()
    {
        using var policy = File.OpenRead(Path.Combine("Data", "simple-1.3.wasm"));
        using var factory = new OpaWasmEvaluatorFactory(policy);
        var results = new bool[Iterations];

        for (var i = 0; i < Iterations; i++)
        {
            using var eval = factory.Create();
            eval.SetData(DataInstance);
            results[i] = eval.EvaluatePredicate(InputInstance).Result;

            if (!results[i])
                throw new Exception("Unexpected result");
        }

        return results;
    }

    [Benchmark]
    public bool[] Stream()
    {
        var opts = new WasmPolicyEngineOptions
        {
            CachePath = "./",
        };

        using var policy = File.OpenRead(Path.Combine("Data", "simple-1.3.wasm"));
        using var factory = new OpaWasmEvaluatorFactory(policy, opts);
        var results = new bool[Iterations];

        for (var i = 0; i < Iterations; i++)
        {
            using var eval = factory.Create();
            eval.SetData(DataInstance);
            results[i] = eval.EvaluatePredicate(InputInstance).Result;

            if (!results[i])
                throw new Exception("Unexpected result");
        }

        return results;
    }
}