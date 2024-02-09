using System.Globalization;
using System.Runtime.CompilerServices;

using OpaDotNet.Wasm.Internal;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace OpaDotNet.Wasm.GoCompat;

internal static class DateTimeExtensions
{
    public static DateTimeOffset FromEpochNs(long ns) => DateTimeOffset.UnixEpoch.AddTicks(ns / TimeSpan.NanosecondsPerTick);

    public static DateTimeOffset FromEpochNs(long ns, TimeZoneInfo tz)
        => TimeZoneInfo.ConvertTime(DateTimeOffset.UnixEpoch.AddTicks(ns / TimeSpan.NanosecondsPerTick), tz);

    public static DateTimeOffset FromNs(long ns) => new(ns / TimeSpan.NanosecondsPerTick, TimeSpan.Zero);

    public static long ToEpochNs(this DateTimeOffset d) => (d - DateTimeOffset.UnixEpoch).Ticks * TimeSpan.NanosecondsPerTick;

    private static readonly DateTimeOffset MinSafeUnixDate = DateTimeOffset.UnixEpoch.AddNs(long.MinValue);
    private static readonly DateTimeOffset MaxSafeUnixDate = DateTimeOffset.UnixEpoch.AddNs(long.MaxValue);

    public static long ToSafeEpochNs(this DateTimeOffset d)
    {
        if (d < MinSafeUnixDate || d > MaxSafeUnixDate)
            throw new ArgumentOutOfRangeException(nameof(d), "Date is outside of valid range");

        return d.ToEpochNs();
    }

    public static DateTimeOffset AddNs(this DateTimeOffset d, long ns) => d.AddTicks(ns / TimeSpan.NanosecondsPerTick);

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

    private static bool TryParseRfc3339(
        ReadOnlySpan<char> s,
        DateTimeStyles styles,
        out DateTimeOffset result)
    {
        return DateTimeOffset.TryParseExact(s, Rfc3339Formats, CultureInfo.InvariantCulture, styles, out result);
    }

    public static DateTimeOffset? ParseRfc3339(ReadOnlySpan<char> s, DateTimeStyles styles = DateTimeStyles.AdjustToUniversal)
    {
        if (!DateTimeOffset.TryParseExact(s, Rfc3339Formats, CultureInfo.InvariantCulture, styles, out var result))
            return null;

        return result;
    }

    internal const string Layout = "01/02 03:04:05PM '06 -0700"; // The reference time, in numerical order.
    internal const string Ansic = "Mon Jan _2 15:04:05 2006";
    internal const string UnixDate = "Mon Jan _2 15:04:05 MST 2006";
    internal const string RubyDate = "Mon Jan 02 15:04:05 -0700 2006";
    internal const string Rfc822 = "02 Jan 06 15:04 MST";
    internal const string Rfc822Z = "02 Jan 06 15:04 -0700"; // RFC822 with numeric zone
    internal const string Rfc850 = "Monday, 02-Jan-06 15:04:05 MST";
    internal const string Rfc1123 = "Mon, 02 Jan 2006 15:04:05 MST";
    internal const string Rfc1123Z = "Mon, 02 Jan 2006 15:04:05 -0700"; // RFC1123 with numeric zone
    internal const string Rfc3339 = "2006-01-02T15:04:05Z07:00";
    internal const string Rfc3339Nano = "2006-01-02T15:04:05.999999999Z07:00";
    internal const string Kitchen = "3:04PM";

    // Handy time stamps.
    internal const string StampTs = "Jan _2 15:04:05";
    internal const string StampMilliTs = "Jan _2 15:04:05.000";
    internal const string StampMicroTs = "Jan _2 15:04:05.000000";
    internal const string StampNanoTs = "Jan _2 15:04:05.000000000";
    internal const string DateTimeTs = "2006-01-02 15:04:05";
    internal const string DateOnlyTs = "2006-01-02";
    internal const string TimeOnlyTs = "15:04:05";

    private const string AnsicParse = "ddd MMM d HH:mm:ss.9999999 yyyy";
    private const string UnixDateParse = "ddd MMM _2 HH:mm:ss.9999999 MST yyyy";
    private const string RubyDateParse = "ddd MMM dd HH:mm:ss.9999999 -0700 yyyy";
    private const string Rfc822Parse = "dd MMM yy HH:mm MST";
    private const string Rfc822ZParse = "dd MMM yy HH:mm -0700"; // RFC822 with numeric zone
    private const string Rfc850Parse = "Monday, dd-MMM-yy HH:mm:ss.9999999 MST";
    private const string Rfc1123Parse = "ddd, dd MMM yyyy HH:mm:ss.9999999 MST";
    private const string Rfc1123ZParse = "ddd, dd MMM yyyy HH:mm:ss.9999999 -0700"; // RFC1123 with numeric zone
    private const string Rfc3339NanoParse = "yyyy-MM-ddTHH:mm:ss.999999999K";

    public static string Format(this DateTimeOffset d, ReadOnlySpan<char> layout, string? timeZoneId = null)
    {
        var format = GetFormatString(d, layout, timeZoneId ?? TimeZoneInfo.Utc.Id);
        return d.ToString(format, CultureInfo.InvariantCulture);
    }

    public static string GetFormatString(DateTimeOffset d, ReadOnlySpan<char> layout)
        => GetFormatString(d, layout, TimeZoneInfo.Utc.Id);

    /// <summary>
    /// GoLang time formatting is completely different from what you usually see in dotnet.
    /// And, oh boy, we're in for a ride. See <see cref="ChunkEnumerator.Enumerator.NextChunk"/> for all gory
    /// details. The main idea is we are trying to build format string compatible with
    /// https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings.
    /// If something can't be handled by <see cref="DateTimeOffset.ToString()"/>
    /// (like __2 aka number days in a year or time zone names) we have to do formatting manually.
    /// </summary>
    public static string GetFormatString(
        DateTimeOffset d,
        ReadOnlySpan<char> layout,
        ReadOnlySpan<char> timeZoneId)
    {
        var inititalBufLen = Math.Max(layout.Length, "9999 September 31 23 59 59 999999999".Length) + timeZoneId.Length;

        var result = new ValueStringBuilder(inititalBufLen);

        Span<char> timeZoneBuffer = stackalloc char[@"hh\:mm\:ss".Length];

        foreach (var chunk in new ChunkEnumerator(layout))
        {
            if (!chunk.Prefix.IsEmpty)
                result.AppendQuoted(chunk.Prefix);

            switch (chunk.Fmt)
            {
                case "pm":
                    if (d.TimeOfDay.Hours is >= 0 and < 12)
                        result.Append("a\\m");
                    else
                        result.Append("p\\m");

                    break;
                case "PM":
                    if (d.TimeOfDay.Hours is >= 0 and < 12)
                        result.Append("A\\M");
                    else
                        result.Append("P\\M");

                    break;
                case "__2":
                    if (d.DayOfYear < 10)
                        result.Append(' ');

                    if (d.DayOfYear < 100)
                        result.Append(' ');

                    result.Append(d.DayOfYear);

                    break;
                case "002":
                    result.Append(d.DayOfYear, "000");

                    break;
                case "_2":
                    if (d.Day < 10)
                        result.Append(' ');

                    result.Append(d.Day);

                    break;
                case "MST":
                    result.AppendQuoted(timeZoneId);
                    break;
                case "fffffffff":
                    result.Append(d.Nanosecond, "'ffffff'000");
                    break;
                case "FFFFFFFFF":
                    if (d.Nanosecond == 0)
                        result.Append("FFFFFF");
                    else
                    {
                        var tns = d.Nanosecond;

                        // Have to trim trailing 0 manually.
                        if (tns % 10 == 0)
                        {
                            tns /= 10;

                            if (tns % 10 == 0)
                                tns /= 10;
                        }

                        result.Append(tns, "'ffffff'#");
                    }

                    break;
                default:
                    if (chunk.Fmt.StartsWith("ZU") && d.Offset == TimeSpan.Zero)
                    {
                        result.Append('Z');
                        break;
                    }

                    if (chunk.Fmt.StartsWith("Z"))
                    {
                        var fmt = chunk.Fmt[2..];

                        if (d.Offset.TryFormat(timeZoneBuffer, out var bw, fmt))
                        {
                            result.Append(d.Offset.Hours < 0 ? '-' : '+');
                            result.Append(timeZoneBuffer[..bw]);
                        }

                        break;
                    }

                    result.Append(chunk.Fmt);
                    break;
            }
        }

        return result.ToString();
    }

    public static bool TryParse(ReadOnlySpan<char> s, ReadOnlySpan<char> layout, out DateTimeOffset result)
    {
        if (layout.SequenceEqual(Rfc3339))
            return TryParseRfc3339(s, DateTimeStyles.None, out result);

        // Known formats are pre-parsed.
        if (layout.SequenceEqual(Rfc3339Nano))
            result = Parse(s, Rfc3339NanoParse);
        else if (layout.SequenceEqual(Ansic))
            result = Parse(s, AnsicParse);
        else if (layout.SequenceEqual(UnixDate))
            result = Parse(s, UnixDateParse);
        else if (layout.SequenceEqual(RubyDate))
            result = Parse(s, RubyDateParse);
        else if (layout.SequenceEqual(Rfc1123))
            result = Parse(s, Rfc1123Parse);
        else if (layout.SequenceEqual(Rfc1123Z))
            result = Parse(s, Rfc1123ZParse);
        else if (layout.SequenceEqual(Rfc850))
            result = Parse(s, Rfc850Parse);
        else if (layout.SequenceEqual(Rfc822))
            result = Parse(s, Rfc822Parse);
        else if (layout.SequenceEqual(Rfc822Z))
            result = Parse(s, Rfc822ZParse);
        else
            result = Parse(s, layout, true);

        return true;
    }

    private static DateTimeOffset Parse(ReadOnlySpan<char> s, ReadOnlySpan<char> layout, bool customFormat = false)
    {
        scoped ValueStringBuilder formatBuilder;

        // ReSharper disable once TooWideLocalVariableScope
        scoped Span<char> buffer;
        const int maxStackSize = 256;

        if (layout.Length > maxStackSize)
            formatBuilder = new(layout.Length);
        else
        {
            buffer = stackalloc char[maxStackSize];
            formatBuilder = new(buffer);
        }

        var parseIndex = 0;

        long nanoseconds = 0;
        var timeZone = TimeSpan.Zero;
        var dayOfYear = -1;
        var hasDay = !customFormat;
        var hasMonth = !customFormat;
        var hasYear = !customFormat;

        foreach (var chunk in new ChunkEnumerator(layout))
        {
            if (!chunk.Prefix.IsEmpty)
            {
                if (customFormat)
                    formatBuilder.AppendQuoted(chunk.Prefix);
                else
                    formatBuilder.Append(chunk.Prefix);

                parseIndex += chunk.Prefix.Length;
            }

            if (chunk.Fmt.StartsWith("y"))
                hasYear = true;

            if (chunk.Fmt.StartsWith("d"))
                hasDay = true;

            if (chunk.Fmt.StartsWith("M"))
                hasMonth = true;

            // Either ',' or '.' are valid as fraction separator. But for DateTime.Parse we need full match.
            if (chunk.Fmt.StartsWith("F") || chunk.Fmt.StartsWith("f"))
            {
                if (s[parseIndex - 1] == '.' || s[parseIndex - 1] == ',')
                {
                    if (customFormat)
                    {
                        // Last char is " (quote), ^2 is separator.
                        formatBuilder[^2] = s[parseIndex - 1];
                    }
                    else
                    {
                        // Last char is separator.
                        formatBuilder[^1] = s[parseIndex - 1];
                    }
                }
            }

            // Will have to handle nanoseconds manually.
            if (chunk.Fmt.StartsWith("fffffffff") || chunk.Fmt.StartsWith("FFFFFFFFF"))
            {
                var parse = s.Slice(parseIndex, chunk.Fmt.Length);
                nanoseconds = long.Parse(parse);
                formatBuilder.Append(parse);
                parseIndex += parse.Length - 1;

                continue;
            }

            if (chunk.Fmt.StartsWith("F") || chunk.Fmt.StartsWith("f"))
            {
                if (s[parseIndex - 1] == '.' || s[parseIndex - 1] == ',')
                {
                    while (parseIndex < s.Length && char.IsDigit(s[parseIndex]))
                        parseIndex++;
                }
                else
                {
                    // Trim separator.
                    parseIndex--;
                }

                formatBuilder.Append(chunk.Fmt);

                continue;
            }

            if (chunk.Fmt is "pm" or "PM")
            {
                formatBuilder.Append("tt");
                parseIndex += 2;

                continue;
            }

            if (chunk.Fmt is "_2")
            {
                hasDay = true;
                formatBuilder.Append('d');
                parseIndex += 2;

                continue;
            }

            if (chunk.Fmt is "__2" or "002")
            {
                var parse = s.Slice(parseIndex, 3);

                // Day of the year is always padded.
                if (int.TryParse(parse, CultureInfo.InvariantCulture, out dayOfYear))
                {
                    // Just quote it so it wont get parsed.
                    formatBuilder.AppendQuoted(parse);
                    parseIndex += 3;
                }

                continue;
            }

            if (chunk.Fmt is "dddd" || chunk.Fmt is "MMMM")
            {
                formatBuilder.Append(chunk.Fmt);

                while (parseIndex < s.Length && char.IsLetter(s[parseIndex]))
                    parseIndex++;

                continue;
            }

            if (chunk.Fmt is "d")
            {
                formatBuilder.Append(chunk.Fmt);

                while (parseIndex < s.Length && char.IsDigit(s[parseIndex]))
                    parseIndex++;

                continue;
            }

            if (chunk.Fmt.StartsWith("Z") && chunk.Fmt.Length > 2)
            {
                var offset = chunk.Fmt[2..];
                var parse = s.Slice(parseIndex, offset.Length + 1);

                var offsetStyle = TimeSpanStyles.None;

                if (parse[0] == '-')
                    offsetStyle = TimeSpanStyles.AssumeNegative;

                timeZone = TimeSpan.ParseExact(parse[1..], offset, CultureInfo.InvariantCulture, offsetStyle);
                formatBuilder.Append(parse);
                parseIndex += parse.Length;

                continue;
            }

            if (chunk.Fmt is "MST")
            {
                var zoneIndex = parseIndex + 1;

                while (zoneIndex < s.Length && !char.IsWhiteSpace(s[zoneIndex]))
                    zoneIndex++;

                var parse = s[parseIndex..zoneIndex];

                if (!parse.StartsWith("GMT"))
                    timeZone = TimeZoneInfoExtensions.FindSystemTimeZoneUtcOffset(parse.ToString());
                else
                {
                    var gmt = parse[3..];

                    if (gmt.Length > 0)
                    {
                        var offset = int.Parse(gmt);
                        timeZone = TimeSpan.FromHours(offset);
                    }
                }

                formatBuilder.AppendQuoted(parse);
                parseIndex += parse.Length;

                continue;
            }

            formatBuilder.Append(chunk.Fmt);
            parseIndex += chunk.Fmt.Length;
        }

        var success = DateTimeOffset.TryParseExact(
            s,
            formatBuilder.AsSpan(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
            out var result
            );

        if (!success)
            throw new FormatException($"Failed to parse {s} with format {formatBuilder.AsSpan()}");

        if (!hasYear || !hasMonth || !hasDay)
        {
            result = new DateTimeOffset(
                hasYear ? result.Year : 1,
                hasMonth ? result.Month : 1,
                hasDay ? result.Day : 1,
                result.Hour,
                result.Minute,
                result.Second,
                result.Millisecond,
                result.Microsecond,
                result.Offset
                );
        }

        if (dayOfYear > 0)
        {
            if (!hasMonth && !hasDay)
                result = result.AddDays(dayOfYear - 1);
            else
            {
                // Need to ensure month and/or day match.
                var baseDate = new DateTimeOffset(result.Year, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(dayOfYear - 1);

                if (!hasMonth)
                    result = result.AddMonths(baseDate.Month - 1);

                if (!hasDay)
                    result = result.AddDays(baseDate.Day - 1);

                if (result.Month != baseDate.Month)
                    throw new FormatException($"Day of the year {dayOfYear} doesn't match month {result.Month}");

                if (result.Day != baseDate.Day)
                    throw new FormatException($"Day of the year {dayOfYear} doesn't match day {result.Day}");
            }
        }

        if (nanoseconds > 0)
            result = result.AddNs(nanoseconds);

        if (timeZone != TimeSpan.Zero)
            result = new DateTimeOffset(result.DateTime, timeZone);

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool InRange(in ReadOnlySpan<char> span, int index) => span.Length > index;

    private static readonly string[] ZeroFormats = ["MM", "dd", "hh", "mm", "ss", "yy"];

    private static readonly string[] FractionFormatsZero =
    [
        "f", "ff", "fff", "ffff", "fffff", "ffffff", "fffffff", "ffffffff", "fffffffff"
    ];

    private static readonly string[] FractionFormatsNine =
    [
        "F", "FF", "FFF", "FFFF", "FFFFF", "FFFFFF", "FFFFFFF", "FFFFFFFF", "FFFFFFFFF"
    ];

    private readonly ref struct ChunkEnumerator
    {
        private readonly ReadOnlySpan<char> _layout;

        // ReSharper disable once ConvertToPrimaryConstructor
        public ChunkEnumerator(ReadOnlySpan<char> layout)
        {
            _layout = layout;
        }

        public Enumerator GetEnumerator() => new(_layout);

        // ReSharper disable once MemberCanBePrivate.Local
        internal ref struct Enumerator
        {
            private ReadOnlySpan<char> _layout;
            private LayoutChunk _chunk;

            // ReSharper disable once ConvertToPrimaryConstructor
            public Enumerator(ReadOnlySpan<char> layout)
            {
                _layout = layout;
                _chunk = new(ReadOnlySpan<char>.Empty, 0, ReadOnlySpan<char>.Empty);
            }

            public LayoutChunk Current => _chunk;

            public bool MoveNext()
            {
                if (_chunk.Suffix.Start.Value >= _layout.Length)
                    return false;

                _layout = _layout[_chunk.Suffix];

                if (_layout.Length == 0)
                    return false;

                _chunk = NextChunk(_layout);

                return true;
            }

            /// <summary>
            /// This is port of https://cs.opensource.google/go/go/+/refs/tags/go1.21.6:src/time/format.go with
            /// dotnet specifics included.
            /// </summary>
            private static LayoutChunk NextChunk(ReadOnlySpan<char> layout)
            {
                for (var i = 0; i < layout.Length; i++)
                {
                    switch (layout[i])
                    {
                        case 'J':
                            if (layout[i..].StartsWith("Jan"))
                            {
                                if (layout[i..].StartsWith("January"))
                                    return new(layout[..i], 7, "MMMM");

                                if (!char.IsLower(layout[i + 3]))
                                    return new(layout[..i], 3, "MMM");
                            }

                            break;

                        case 'M':
                            if (layout[i..].StartsWith("Mon"))
                            {
                                if (layout[i..].StartsWith("Monday"))
                                    return new(layout[..i], 6, "dddd");

                                if (!char.IsLower(layout[i + 3]))
                                    return new(layout[..i], 3, "ddd");
                            }

                            if (layout[i..].StartsWith("MST"))
                                return new(layout[..i], 3, "MST");

                            break;

                        case '0':
                            if (InRange(layout, i + 1) && layout[i + 1] >= '1' && layout[i + 1] <= '6')
                                return new(layout[..i], 2, ZeroFormats[layout[i + 1] - '1']);

                            if (layout[i..].StartsWith("002"))
                                return new(layout[..i], 3, "002");

                            break;

                        case '1':
                            if (InRange(layout, i + 1) && layout[i + 1] == '5')
                                return new(layout[..i], 2, "HH");

                            return new(layout[..i], 1, "M");

                        case '2':
                            if (layout[i..].StartsWith("2006"))
                                return new(layout[..i], 4, "yyyy");

                            return new(layout[..i], 1, "d");

                        case '_':
                            if (InRange(layout, i + 1) && layout[i + 1] == '2')
                            {
                                if (layout[i..].StartsWith("2006"))
                                    return new(layout[..i], 4, "yyyy");

                                return new(layout[..i], 2, "_2");
                            }

                            if (layout[i..].StartsWith("__2"))
                                return new(layout[..i], 3, "__2");

                            break;

                        case '3':
                            return new(layout[..i], 1, "h");

                        case '4':
                            return new(layout[..i], 1, "m");

                        case '5':
                            return new(layout[..i], 1, "s");

                        case 'P':
                            // Can't use "tt" here because if will force "PM" or "pm" only.
                            if (InRange(layout, i + 1) && layout[i + 1] == 'M')
                                return new(layout[..i], 2, "PM");

                            break;

                        case 'p':
                            // Can't use "tt" here because if will force "PM" or "pm" only.
                            if (InRange(layout, i + 1) && layout[i + 1] == 'm')
                                return new(layout[..i], 2, "pm");

                            break;

                        case '-':
                            if (layout[i..].StartsWith("-070000"))
                                return new(layout[..i], 7, "ZNhhmmss");

                            if (layout[i..].StartsWith("-07:00:00"))
                                return new(layout[..i], 9, "ZNhh\\:mm\\:ss");

                            if (layout[i..].StartsWith("-0700"))
                                return new(layout[..i], 5, "ZNhhmm");

                            if (layout[i..].StartsWith("-07:00"))
                                return new(layout[..i], 6, "ZNhh\\:mm");

                            if (layout[i..].StartsWith("-07"))
                                return new(layout[..i], 3, "ZNhh");

                            break;

                        case 'Z':
                            if (layout[i..].StartsWith("Z070000"))
                                return new(layout[..i], 7, "ZUhhmmss");

                            if (layout[i..].StartsWith("Z07:00:00"))
                                return new(layout[..i], 9, "ZUhh\\:mm\\:ss");

                            if (layout[i..].StartsWith("Z0700"))
                                return new(layout[..i], 5, "ZUhhmm");

                            if (layout[i..].StartsWith("Z07:00"))
                                return new(layout[..i], 6, "ZUhh\\:mm");

                            if (layout[i..].StartsWith("Z07"))
                                return new(layout[..i], 3, "ZUhh");

                            break;

                        case '.' or ',':
                            if (InRange(layout, i + 1) && layout[i + 1] is '0' or '9')
                            {
                                var ch = layout[i + 1];
                                int j;

                                for (j = 1; i + j < layout.Length; j++)
                                {
                                    if (layout[i + j] != ch)
                                        break;
                                }

                                if (j - 2 < FractionFormatsZero.Length)
                                {
                                    // If we got here doesn't mean we have fraction.
                                    // It can be something wit dot separators (aka yyyy.MM.dd).
                                    if (!InRange(layout, i + j) || !char.IsDigit(layout[i + j]))
                                    {
                                        //Span<char> fmt = "";
                                        var fmt = ch == '0' ? FractionFormatsZero[j - 2] : FractionFormatsNine[j - 2];
                                        return new(layout[..(i + 1)], j - 1, fmt);
                                    }
                                }
                            }

                            break;
                    }
                }

                return new(layout, 0, string.Empty);
            }
        }
    }

    private readonly ref struct LayoutChunk(ReadOnlySpan<char> prefix, int suffixStartOffset, ReadOnlySpan<char> format)
    {
        public ReadOnlySpan<char> Prefix { get; } = prefix;

        public Range Suffix { get; } = Range.StartAt(prefix.Length + suffixStartOffset);

        public ReadOnlySpan<char> Fmt { get; } = format;
    }
}