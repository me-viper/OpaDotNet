using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

using OpaDotNet.Benchmarks.Exporters;

namespace OpaDotNet.Benchmarks;

public class Config : ManualConfig
{
    public Config()
    {
        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(new BenchStatExporter());
    }
}