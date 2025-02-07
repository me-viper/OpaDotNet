using System.Globalization;

namespace OpaDotNet.Wasm.GoCompat;

internal readonly struct DateTimeOffsetEx
{
    private const int NanosecondsInFraction = 1_000_000;

    public DateTimeOffset Date { get; }

    public byte TicksFraction { get; }

    /// <summary>
    /// Creates new instance of <see cref="DateTimeOffsetEx"/>
    /// </summary>
    /// <param name="date">Base date.</param>
    /// <param name="fraction">Everything that is less than second.</param>
    public DateTimeOffsetEx(DateTimeOffset date, uint fraction)
    {
        if (fraction > 0)
        {
            while (fraction < NanosecondsInFraction)
                fraction *= 10;
        }

        Date = date.AddTicks(fraction / TimeSpan.NanosecondsPerTick);
        TicksFraction = (byte)(fraction % 100);
    }

    private DateTimeOffsetEx(DateTimeOffset date, byte ticksFraction)
    {
        Date = date;
        TicksFraction = ticksFraction;
    }

    public DateTimeOffsetEx AddNs(long ns)
    {
        int tf;
        var ticks = ns / TimeSpan.NanosecondsPerTick;

        if (ns < 0)
        {
            tf = TicksFraction - (int)(ns % 100);

            if (tf < 0)
            {
                ticks--;
                tf += 100;
            }
        }
        else
        {
            tf = TicksFraction + (int)(ns % 100);

            if (tf > 100)
            {
                ticks++;
                tf -= 100;
            }
        }

        var d = Date.AddTicks(ticks);
        return new DateTimeOffsetEx(d, (byte)tf);
    }

    public static DateTimeOffsetEx FromNs(long ns) => new(DateTimeExtensions.FromNs(ns), (byte)(ns % 100));

    public static DateTimeOffsetEx FromEpochNs(long ns, TimeZoneInfo tz)
    {
        var d = FromEpochNs(ns);
        var dd = TimeZoneInfo.ConvertTime(d.Date, tz);
        return new DateTimeOffsetEx(dd, d.TicksFraction);
    }

    public static DateTimeOffsetEx FromEpochNs(long ns) => new(DateTimeExtensions.FromEpochNs(ns), (byte)(ns % 100));

    public long ToEpochNs() => Date.ToEpochNs() + TicksFraction;

    public long ToSafeEpochNs()
    {
        var r = Date.ToEpochNs128() + TicksFraction;

        if (r < DateTimeExtensions.MinSafeUnixDateNs || r > DateTimeExtensions.MaxSafeUnixDateNs)
            throw new InvalidOperationException("Time outside of valid range");

        return (long)r;
    }

    private static string[] Rfc3339Formats { get; } =
    [
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffffK",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffK",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffK",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffK",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fK",
        "yyyy'-'MM'-'dd'T'HH':'mm':'ssK",

        // Fall back patterns
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK",
        DateTimeFormatInfo.InvariantInfo.UniversalSortableDateTimePattern,
        DateTimeFormatInfo.InvariantInfo.SortableDateTimePattern,
    ];

    private static DateTimeOffset ParseRfc3339(ReadOnlySpan<char> s, DateTimeStyles styles = DateTimeStyles.AdjustToUniversal)
        => DateTimeOffset.ParseExact(s, Rfc3339Formats, CultureInfo.InvariantCulture, styles);

    public static DateTimeOffsetEx ParseRfc3339Ns(ReadOnlySpan<char> s, DateTimeStyles styles = DateTimeStyles.AdjustToUniversal)
    {
        var i = 0;
        var start = -1;
        var end = -1;

        while (i < s.Length)
        {
            if (s[i] == '.')
            {
                start = ++i;

                while (i < s.Length)
                {
                    if (!char.IsDigit(s[i]))
                        break;

                    end = i;
                    i++;
                }
            }

            i++;
        }

        end++;

        if (end - start <= 7)
            return new(ParseRfc3339(s), 0);

        if (!uint.TryParse(s[start..end], out var fractionNs))
            throw new FormatException("Invalid fraction");

        // Get date without fraction.
        var snf = string.Concat(s[..(start - 1)], s[end..]);

        var date = DateTimeOffset.ParseExact(snf, Rfc3339Formats, CultureInfo.InvariantCulture, styles);

        return new DateTimeOffsetEx(date, fractionNs);
    }

    public string Format(ReadOnlySpan<char> layout, string? timeZoneId = null)
    {
        var format = DateTimeExtensions.GetFormatString(this, layout, timeZoneId ?? TimeZoneInfo.Utc.Id);
        return Date.ToString(format, CultureInfo.InvariantCulture);
    }
}