using System.Globalization;
using System.Reflection;

using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace OpaDotNet.Benchmarks.Exporters;

public class BenchStatExporterAttribute() : ExporterConfigBaseAttribute(new BenchStatExporter());

/// <summary>
/// Exports benchmarks in format compatible with
/// benchstat (https://go.googlesource.com/proposal/+/master/design/14313-benchmark-format.md).
/// </summary>
public class BenchStatExporter : ExporterBase
{
    public static readonly IExporter Default = new BenchStatExporter();

    public override void ExportToLog(Summary summary, ILogger logger)
    {
        var pkg = Assembly.GetEntryAssembly()!.GetName().Name;
        var culture = CultureInfo.InvariantCulture;

        logger.WriteLine($"os: {summary.HostEnvironmentInfo.Os.Value}");
        logger.WriteLine($"arch: {summary.HostEnvironmentInfo.Architecture}");
        logger.WriteLine($"pkg: {pkg}");
        logger.WriteLine($"cpu: {summary.HostEnvironmentInfo.Cpu.Value.ProcessorName}");
        logger.WriteLine($"cores: {summary.HostEnvironmentInfo.Cpu.Value.LogicalCoreCount ?? 0}");
        logger.WriteLine($"net: {string.Join(';', summary.HostEnvironmentInfo.DotNetSdkVersion)}");

        foreach (var report in summary.Reports)
        {
            string[] name =
            [
                report.BenchmarkCase.Descriptor.DisplayInfo,
                report.BenchmarkCase.Parameters.DisplayInfo,
            ];

            var bench = string.Join('_', name).TrimEnd('_');

            var runs = report.GetResultRuns();

            if (runs.Count == 0)
                continue;

            var bytesPerOp = (report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase) ?? 0).ToString(culture);
            var gen0 = (report.GcStats.GetCollectionsCount(0) / (double)report.GcStats.TotalOperations * 1000).ToString(culture);
            var gen1 = (report.GcStats.GetCollectionsCount(1) / (double)report.GcStats.TotalOperations * 1000).ToString(culture);
            var gen2 = (report.GcStats.GetCollectionsCount(2) / (double)report.GcStats.TotalOperations * 1000).ToString(culture);

            foreach (var run in runs)
            {
                var iterations = run.Operations;
                var nsPerOp = run.GetAverageTime().Nanoseconds.ToString(culture);

                string[] statsLog =
                [
                    $"Benchmark{bench}",
                    $"{iterations}",
                    $"{nsPerOp} ns/op",
                    $"{bytesPerOp} B/op",
                    $"{gen0} gen0/op/1k",
                    $"{gen1} gen1/op/1k",
                    $"{gen2} gen2/op/1k",
                ];

                logger.WriteLine(string.Join("    ", statsLog));
            }
        }
    }
}