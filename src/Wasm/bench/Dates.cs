using BenchmarkDotNet.Attributes;

using OpaDotNet.Wasm.GoCompat;

namespace OpaDotNet.Benchmarks;

[Config(typeof(Config))]
public class Dates
{
    private static readonly DateTimeOffset TheDate = TimeZoneInfo.ConvertTime(
        DateTimeExtensions.FromEpochNs(1233810057012345600),
        TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("Pacific Standard Time")
        );

    private static readonly TimeZoneInfo TheDateZone = TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("PST");

    [Benchmark]
    [BenchmarkCategory("Format")]
    public string FormatUnix()
    {
        return TheDate.Format(DateTimeExtensions.UnixDate, TheDateZone.Id);
    }

    [Benchmark]
    [BenchmarkCategory("Format")]
    public string FormatRfc3339()
    {
        return TheDate.Format(DateTimeExtensions.Rfc3339, TheDateZone.Id);
    }

    [Benchmark]
    [BenchmarkCategory("Format")]
    public string FormatRfc3339Nano()
    {
        return TheDate.Format(DateTimeExtensions.Rfc3339Nano, TheDateZone.Id);
    }

    [Benchmark]
    [BenchmarkCategory("Parse")]
    public DateTimeOffset ParseUnix()
    {
        var success = DateTimeExtensions.TryParseNs(
            "Fri Feb  5 05:00:57 GMT-8 2010",
            DateTimeExtensions.UnixDate,
            out var result
            );

        if (!success)
            throw new InvalidOperationException("Failed");

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Parse")]
    public DateTimeOffset ParseRfc3339()
    {
        var success = DateTimeExtensions.TryParseNs(
            "2010-02-04T21:00:57-08:00",
            DateTimeExtensions.Rfc3339,
            out var result
            );

        if (!success)
            throw new InvalidOperationException("Failed");

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Parse")]
    public DateTimeOffset ParseRfc3339Nano()
    {
        var success = DateTimeExtensions.TryParseNs(
            "2010-02-04T21:00:57.012345678-08:00",
            DateTimeExtensions.Rfc3339Nano,
            out var result
            );

        if (!success)
            throw new InvalidOperationException("Failed");

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Parse")]
    public DateTimeOffset ParseCustomTzPst()
    {
        var success = DateTimeExtensions.TryParseNs(
            "Thursday, 10.February.04 05:00:57.012 PST",
            "Monday, 06.January.02 15:04:05.999 MST",
            out var result
            );

        if (!success)
            throw new InvalidOperationException("Failed");

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Parse")]
    public DateTimeOffset ParseCustomTzAmericaLa()
    {
        var success = DateTimeExtensions.TryParseNs(
            "Thursday, 10.February.04 05:00:57.012 America/Los_Angeles",
            "Monday, 06.January.02 15:04:05.999 MST",
            out var result
            );

        if (!success)
            throw new InvalidOperationException("Failed");

        return result;
    }

    [Benchmark]
    [BenchmarkCategory("Parse")]
    public DateTimeOffset ParseCustomNoTz()
    {
        var success = DateTimeExtensions.TryParseNs(
            "Thursday, 10.February.04 05:00:57.012",
            "Monday, 06.January.02 15:04:05.999",
            out var result
            );

        if (!success)
            throw new InvalidOperationException("Failed");

        return result;
    }
}