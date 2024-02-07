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

    private static int[] Date(long ns)
    {
        var result = DateTimeExtensions.FromEpochNs(ns);
        return [result.Year, result.Month, result.Day];
    }

    private static int[] Clock(long ns)
    {
        var result = DateTimeExtensions.FromEpochNs(ns);
        return [result.Hour, result.Minute, result.Second];
    }

    private static int[] Diff(long ns1, long ns2)
    {
        var d = DateTimeExtensions.FromNs(ns1 - ns2);

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
            .ToEpochNs();
    }

    private static string Weekday(long ns)
    {
        var result = DateTimeOffset.UnixEpoch.AddTicks(ns / 100);
        return result.DayOfWeek.ToString("G");
    }

    private static long? ParseDurationNs(string duration)
    {
        double time;

        // Valid time units are "ns", "us" (or "µs"), "ms", "s", "m", "h".
        if (duration[^2..] == "ms")
        {
            time = double.Parse(duration[..^2], CultureInfo.InvariantCulture);
            return (long)TimeSpan.FromSeconds(time).TotalNanoseconds;
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

    private static long? ParseRfc3339Ns(string s)
    {
        var result = DateTimeExtensions.ParseRfc3339(s);
        return result?.ToEpochNs();
    }

    private static readonly IReadOnlyDictionary<string, string> TimeFormats = new Dictionary<string, string>
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

    private static string? TimeFormat(JsonNode? x)
    {
        if (x == null)
            return null;

        long ns;
        var format = DateTimeExtensions.Rfc3339Nano;
        DateTimeOffset date;

        if (x is JsonValue jv)
        {
            if (!jv.TryGetValue(out ns))
                return null;

            date = DateTimeExtensions.FromEpochNs(ns);
            return date.Format(format);
        }

        if (x is not JsonArray ja || ja.Count < 2)
            return null;

        ns = ja[0]!.GetValue<long>();
        var timeZone = ja[1]!.GetValue<string>();

        if (ja.Count == 3)
            format = ja[2]!.GetValue<string>();

        if (TimeFormats.TryGetValue(format, out var f))
            format = f;

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

        date = DateTimeExtensions.FromEpochNs(ns, tz);

        return date.Format(format, zoneId);
    }

    private static long? TimeParseNs(string layout, string value)
    {
        if (TimeFormats.TryGetValue(layout, out var f))
            layout = f;

        if (!DateTimeExtensions.TryParse(value, layout, out var result))
            return null;

        return result.ToEpochNs();
    }
}