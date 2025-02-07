using System.Globalization;
using System.Text.Json.Nodes;

using OpaDotNet.Wasm.GoCompat;

namespace OpaDotNet.Wasm;

public partial class DefaultOpaImportsAbi
{
    private long NowNs() => CacheGetOrAddValue(
        "time.now_ns",
        () => Now().ToEpochNs()
        );

    private static int[] Date(JsonNode? nsArg)
    {
        ArgumentNullException.ThrowIfNull(nsArg);

        if (nsArg is not JsonArray ja)
        {
            var n = nsArg.GetValue<long>();
            return Date(n);
        }

        var (ns, tz) = ParseArgs<long, string>(ja);

        var tzi = TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr(tz);
        var t = DateTimeExtensions.FromEpochNs(ns, tzi);

        return [t.Year, t.Month, t.Day];
    }

    private static int[] Date(long ns)
    {
        var result = DateTimeExtensions.FromEpochNs(ns);
        return [result.Year, result.Month, result.Day];
    }

    private static int[] Clock(JsonNode? nsArg)
    {
        ArgumentNullException.ThrowIfNull(nsArg);

        if (nsArg is not JsonArray ja)
        {
            var n = nsArg.GetValue<long>();
            return Clock(n);
        }

        var (ns, tz) = ParseArgs<long, string>(ja);

        var tzi = TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr(tz);
        var t = DateTimeExtensions.FromEpochNs(ns, tzi);

        return [t.Hour, t.Minute, t.Second];
    }

    private static int[] Clock(long ns)
    {
        var result = DateTimeExtensions.FromEpochNs(ns);
        return [result.Hour, result.Minute, result.Second];
    }

    private static int[] Diff(JsonNode? nsArg1, JsonNode? nsArg2)
    {
        ArgumentNullException.ThrowIfNull(nsArg1);
        ArgumentNullException.ThrowIfNull(nsArg2);

        var ns1 = nsArg1 is not JsonArray ja1 ? nsArg1.GetValue<long>() : ParseDiffArg(ja1);
        var ns2 = nsArg2 is not JsonArray ja2 ? nsArg2.GetValue<long>() : ParseDiffArg(ja2);

        return Diff(ns1, ns2);
    }

    private static long ParseDiffArg(JsonArray arg)
    {
        var (ns, tz) = ParseArgs<long, string>(arg);

        var tzi = TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr(tz);
        var t = DateTimeExtensions.FromEpochNs(ns, tzi);

        return t.ToUniversalTime().ToSafeEpochNs();
    }

    private static int[] Diff(long ns1, long ns2)
    {
        var d = DateTimeExtensions.FromNs(Math.Abs(ns1 - ns2));

        return
        [
            d.Year - 1,
            d.Month - 1,
            d.Day - 1,
            d.Hour,
            d.Minute,
            d.Second,
        ];
    }

    private static long AddDate(long ns, int years, int months, int days)
    {
        return DateTimeExtensions.FromEpochNs(ns)
            .AddYears(years)
            .AddMonths(months)
            .AddDays(days)
            .ToSafeEpochNs();
    }

    private static string Weekday(JsonNode? nsArg)
    {
        ArgumentNullException.ThrowIfNull(nsArg);

        if (nsArg is not JsonArray ja)
        {
            var n = nsArg.GetValue<long>();
            return Weekday(n);
        }

        var (ns, tz) = ParseArgs<long, string>(ja);

        var tzi = TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr(tz);
        var t = DateTimeExtensions.FromEpochNs(ns, tzi);

        return t.DayOfWeek.ToString("G");
    }

    private static string Weekday(long ns)
    {
        var result = DateTimeExtensions.FromEpochNs(ns);
        return result.DayOfWeek.ToString("G");
    }

    internal static long? ParseDurationNs(string duration)
    {
        double time;

        // Valid time units are "ns", "us" (or "µs"), "ms", "s", "m", "h".
        if (duration[^2..] == "ms")
        {
            time = double.Parse(duration[..^2], CultureInfo.InvariantCulture);
            return (long)TimeSpan.FromMilliseconds(time).TotalNanoseconds;
        }

        if (duration[^2..] == "us" || duration[^2..] == "µs")
        {
            time = double.Parse(duration[..^2], CultureInfo.InvariantCulture);
            return (long)TimeSpan.FromMicroseconds(time).TotalNanoseconds;
        }

        if (duration[^2..] == "ns")
        {
            time = double.Parse(duration[..^2], CultureInfo.InvariantCulture);
            return (long)time;
        }

        if (duration[^1] == 'h')
        {
            time = double.Parse(duration[..^1], CultureInfo.InvariantCulture);
            return (long)TimeSpan.FromHours(time).TotalNanoseconds;
        }

        if (duration[^1] == 'm')
        {
            time = double.Parse(duration[..^1], CultureInfo.InvariantCulture);
            return (long)TimeSpan.FromMinutes(time).TotalNanoseconds;
        }

        if (duration[^1] == 's')
        {
            time = double.Parse(duration[..^1], CultureInfo.InvariantCulture);
            return (long)TimeSpan.FromSeconds(time).TotalNanoseconds;
        }

        return null;
    }

    private static long? ParseRfc3339Ns(string s) => DateTimeOffsetEx.ParseRfc3339Ns(s).ToSafeEpochNs();

    private static readonly Dictionary<string, string> TimeFormats = new()
    {
        { "ANSIC", DateTimeExtensions.Ansic },
        { "UnixDate", DateTimeExtensions.UnixDate },
        { "RubyDate", DateTimeExtensions.RubyDate },
        { "RFC822", DateTimeExtensions.Rfc822 },
        { "RFC822Z", DateTimeExtensions.Rfc822Z },
        { "RFC850", DateTimeExtensions.Rfc850 },
        { "RFC1123", DateTimeExtensions.Rfc1123 },
        { "RFC1123Z", DateTimeExtensions.Rfc1123Z },
        { "RFC3339", DateTimeExtensions.Rfc3339 },
        { "RFC3339Nano", DateTimeExtensions.Rfc3339Nano },
    };

    private static string TimeFormat(JsonNode? x)
    {
        ArgumentNullException.ThrowIfNull(x);

        long ns;
        var format = DateTimeExtensions.Rfc3339Nano;
        DateTimeOffsetEx date;

        if (x is JsonValue jv)
        {
            if (!jv.TryGetValue(out ns))
                throw new FormatException("Invalid time");

            date = DateTimeOffsetEx.FromEpochNs(ns);
            return date.Format(format);
        }

        if (x is not JsonArray ja)
            throw new ArgumentException("JsonArray expected", nameof(x));

        (ns, var timeZone, format) = ParseArgs<long, string?, string?>(ja, 2);

        if (string.IsNullOrWhiteSpace(format))
            format = DateTimeExtensions.Rfc3339Nano;
        else
        {
            if (TimeFormats.TryGetValue(format, out var f))
                format = f;
        }

        TimeZoneInfo? tz;
        var zoneId = timeZone;

        if (string.IsNullOrEmpty(timeZone))
        {
            tz = TimeZoneInfo.Utc;
            zoneId = tz.Id;
        }
        else if (string.Equals(timeZone, "Local", StringComparison.Ordinal))
        {
            tz = TimeZoneInfo.Local;
            zoneId = tz.Id;
        }
        else
        {
            tz = TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr(timeZone);
        }

        date = DateTimeOffsetEx.FromEpochNs(ns, tz);

        return date.Format(format, zoneId);
    }

    internal static long? TimeParseNs(string layout, string value)
    {
        if (TimeFormats.TryGetValue(layout, out var f))
            layout = f;

        return DateTimeExtensions.ParseNs(value, layout).ToSafeEpochNs();
    }
}