using System.Globalization;
using System.Reflection;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
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

        logger.WriteLine($"os: {summary.HostEnvironmentInfo.OsVersion.Value}");
        logger.WriteLine($"arch: {summary.HostEnvironmentInfo.Architecture}");
        logger.WriteLine($"pkg: {pkg}");
        logger.WriteLine($"cpu: {summary.HostEnvironmentInfo.CpuInfo.Value.ProcessorName}");
        logger.WriteLine($"cores: {summary.HostEnvironmentInfo.CpuInfo.Value.LogicalCoreCount ?? 0}");
        logger.WriteLine($"net: {string.Join(';', summary.HostEnvironmentInfo.DotNetSdkVersion)}");

        foreach (var report in summary.Reports)
        {
            string[] name = [
                report.BenchmarkCase.Descriptor.DisplayInfo,
                //report.BenchmarkCase.Job.DisplayInfo,
                report.BenchmarkCase.Parameters.DisplayInfo,
            ];

            var bench = string.Join('_', name).TrimEnd('_');

            var runs = report.GetResultRuns();
            var stats = runs.GetStatistics();

            if (runs.Count == 0)
                continue;

            var iterations = runs[0].Operations;
            var nsPerOp = stats.Mean.ToString(culture);
            var bytesPerOp = (report.GcStats.GetBytesAllocatedPerOperation(report.BenchmarkCase) ?? 0).ToString(culture);
            var gen0 = (report.GcStats.GetCollectionsCount(0) / (double) report.GcStats.TotalOperations * 1000).ToString(culture);
            var gen1 = (report.GcStats.GetCollectionsCount(1) / (double) report.GcStats.TotalOperations * 1000).ToString(culture);
            var gen2 = (report.GcStats.GetCollectionsCount(2) / (double) report.GcStats.TotalOperations * 1000).ToString(culture);

            string[] statsLog = [
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