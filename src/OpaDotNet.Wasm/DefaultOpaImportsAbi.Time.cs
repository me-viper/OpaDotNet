using System.Globalization;

using DateTimeOffset = System.DateTimeOffset;

namespace OpaDotNet.Wasm;

public partial class DefaultOpaImportsAbi
{
    private static long NowNs(DateTimeOffset now)
    {
        return (now.Ticks - DateTimeOffset.UnixEpoch.Ticks) * 100;
    }

    private static int[] Date(long ns)
    {
        var result = DateTimeOffset.UnixEpoch.AddTicks(ns / 100);
        return new[] { result.Year, result.Month, result.Day };
    }

    private static int[] Clock(long ns)
    {
        var result = DateTimeOffset.UnixEpoch.AddTicks(ns / 100);
        return new[] { result.Hour, result.Minute, result.Second };
    }

    private static int[] Diff(long ns1, long ns2)
    {
        var diffTicks = (ns1 - ns2) / 100;
        var d = new DateTimeOffset(diffTicks, TimeSpan.Zero);

        return new[]
        {
            d.Year - 1,
            d.Month - 1,
            d.Day - 1,
            d.Hour,
            d.Minute,
            d.Second,
        };
    }

    private static long AddDate(long ns, int years, int months, int days)
    {
        var result = DateTimeOffset.UnixEpoch
            .AddTicks(ns / 100)
            .AddYears(years)
            .AddMonths(months)
            .AddDays(days);
        return (result.Ticks - DateTimeOffset.UnixEpoch.Ticks) * 100;
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
            return TimeSpan.FromSeconds(time).Ticks * 100;
        }
        
        if (duration[^2..] == "us" || duration[^2..] == "µs")
        {
            time = double.Parse(duration[..^2], CultureInfo.InvariantCulture);
            return TimeSpan.FromMicroseconds(time).Ticks * 100;
        }
        
        if (duration[^2..] == "ns")
        {
            time = double.Parse(duration[..^2], CultureInfo.InvariantCulture);
            return (long)time;
        }
        
        if (duration[^1] == 'h')
        {
            time = double.Parse(duration[..^1], CultureInfo.InvariantCulture);
            return TimeSpan.FromHours(time).Ticks * 100;
        }
        
        if (duration[^1] == 'm')
        {
            time = double.Parse(duration[..^1], CultureInfo.InvariantCulture);
            return TimeSpan.FromMinutes(time).Ticks * 100;
        }

        if (duration[^1] == 's')
        {
            time = double.Parse(duration[..^1], CultureInfo.InvariantCulture);
            return TimeSpan.FromSeconds(time).Ticks * 100;
        }

        return null;
    }
    
    private static long ParseRfc3339Ns(string s)
    {
        throw new NotImplementedException();
    }
}