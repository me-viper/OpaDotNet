using System.Globalization;

using OpaDotNet.Wasm.GoCompat;

namespace OpaDotNet.Wasm.Tests;

public class DateTimeExtensionsTests(ITestOutputHelper output)
{
    public record FormatTestCase(string Name, string Format, string Expected)
    {
        public override string ToString() => Name;
    }

    public class FormatTheoryData : TheoryData<FormatTestCase>
    {
        public FormatTheoryData()
        {
            AddRange(
                [
                    new("am/pm", "3pm", "9pm"),
                    new("AM/PM", "3PM", "9PM"),
                    new("pm/PM", "3PM=3pm", "9PM=9pm"),
                    new("YearDay", "Jan  2 002 __2 2", "Feb  4 035  35 4"),
                    new("Year", "2006 6 06 _6 __6 ___6", "2009 6 09 _6 __6 ___6"),
                    new("Month", "Jan January 1 01 _1", "Feb February 2 02 _2"),
                    new("DayOfMonth", "2 02 _2 __2", "4 04  4  35"),
                    new("DayOfWeek", "Mon Monday", "Wed Wednesday"),
                    new("Hour", "15 3 03 _3", "21 9 09 _9"),
                    new("Minute", "4 04 _4", "0 00 _0"),
                    new("Second", "5 05 _5", "57 57 _57"),
                    new("ANSIC", DateTimeExtensions.Ansic, "Wed Feb  4 21:00:57 2009"),
                    new("UnixDate", DateTimeExtensions.UnixDate, "Wed Feb  4 21:00:57 PST 2009"),
                    new("RubyDate", DateTimeExtensions.RubyDate, "Wed Feb 04 21:00:57 -0800 2009"),
                    new("RFC822", DateTimeExtensions.Rfc822, "04 Feb 09 21:00 PST"),
                    new("RFC850", DateTimeExtensions.Rfc850, "Wednesday, 04-Feb-09 21:00:57 PST"),
                    new("RFC1123", DateTimeExtensions.Rfc1123, "Wed, 04 Feb 2009 21:00:57 PST"),
                    new("RFC1123Z", DateTimeExtensions.Rfc1123Z, "Wed, 04 Feb 2009 21:00:57 -0800"),
                    new("RFC3339", DateTimeExtensions.Rfc3339, "2009-02-04T21:00:57-08:00"),
                    new("RFC3339Nano", DateTimeExtensions.Rfc3339Nano, "2009-02-04T21:00:57.0123456-08:00"),
                    new("Kitchen", DateTimeExtensions.Kitchen, "9:00PM"),
                    new("am/pm", "3pm", "9pm"),
                    new("AM/PM", "3PM", "9PM"),
                    new("two-digit year", "06 01 02", "09 02 04"),
                    new("Janet", "Hi Janet, the Month is January", "Hi Janet, the Month is February"),
                    new("Stamp", DateTimeExtensions.StampTs, "Feb  4 21:00:57"),
                    new("StampMilli", DateTimeExtensions.StampMilliTs, "Feb  4 21:00:57.012"),
                    new("StampMicro", DateTimeExtensions.StampMicroTs, "Feb  4 21:00:57.012345"),
                    new("StampNano", DateTimeExtensions.StampNanoTs, "Feb  4 21:00:57.012345600"),
                    new("StampNano Trim", "Jan _2 15:04:05.999999999", "Feb  4 21:00:57.0123456"),
                    new("DateTime", DateTimeExtensions.DateTimeTs, "2009-02-04 21:00:57"),
                    new("DateOnly", DateTimeExtensions.DateOnlyTs, "2009-02-04"),
                    new("TimeOnly", DateTimeExtensions.TimeOnlyTs, "21:00:57"),
                    new("Dots", "2006.01.02 15:04:05.999", "2009.02.04 21:00:57.012"),
                ]
                );
        }
    }

    private static DateTimeOffset TheDate { get; } = TimeZoneInfo.ConvertTime(
        DateTimeExtensions.FromEpochNs(1233810057012345600),
        TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("Pacific Standard Time")
        );

    [Fact]
    public void SingleDigits()
    {
        var d = new DateTimeOffset(2001, 2, 3, 4, 5, 6, 700, TimeSpan.Zero);
        var f = DateTimeExtensions.GetFormatString(d, "3:4:5");

        output.WriteLine(f);
        Assert.Equal("4:5:6", d.ToString(f, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Rfc339Nano()
    {
        var tz = TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("America/New_York");
        var date = DateTimeExtensions.FromEpochNs(1707133074028819500, tz);
        var f = DateTimeExtensions.GetFormatString(date, DateTimeExtensions.Rfc3339Nano, tz.Id);

        output.WriteLine(f);
        Assert.Equal("2024-02-05T06:37:54.0288195-05:00", date.ToString(f, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void NanoTrim()
    {
        var tz = TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("America/New_York");
        var date = DateTimeExtensions.FromEpochNs(1707133074028819512, tz);
        var f = DateTimeExtensions.GetFormatString(date, DateTimeExtensions.Rfc3339Nano, tz.Id);

        output.WriteLine(f);

        // Although nanosecond is 512 it will actually be 500 because
        // DateTime.Nanosecond is actually DateTime.Ticks * 100;
        Assert.Equal("2024-02-05T06:37:54.0288195-05:00", date.ToString(f, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Rfc822()
    {
        var tz = TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("EST");
        var date = DateTimeExtensions.FromEpochNs(1707133074028819500, tz);
        var f = DateTimeExtensions.GetFormatString(date, DateTimeExtensions.Rfc822, "EST");

        output.WriteLine(f);
        Assert.Equal("05 Feb 24 06:37 EST", date.ToString(f, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Rfc3339Utc()
    {
        var date = new DateTimeOffset(2008, 9, 17, 20, 4, 26, 0, TimeSpan.Zero);
        var f = DateTimeExtensions.GetFormatString(date, DateTimeExtensions.Rfc3339);

        output.WriteLine(f);
        Assert.Equal("2008-09-17T20:04:26Z", date.ToString(f, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Rfc3339Est()
    {
        var tz = TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("EST");
        var date = new DateTimeOffset(1994, 9, 17, 20, 4, 26, 0, tz.BaseUtcOffset);
        var f = DateTimeExtensions.GetFormatString(date, DateTimeExtensions.Rfc3339);

        output.WriteLine(f);
        Assert.Equal("1994-09-17T20:04:26-05:00", date.ToString(f, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Rfc3339Oto()
    {
        var date = new DateTimeOffset(2000, 9, 17, 20, 4, 26, 0, new(4, 20, 0));
        var f = DateTimeExtensions.GetFormatString(date, DateTimeExtensions.Rfc3339);

        output.WriteLine(f);
        Assert.Equal("2000-09-17T20:04:26+04:20", date.ToString(f, CultureInfo.InvariantCulture));
    }

    [Theory]
    [ClassData(typeof(FormatTheoryData))]
    public void Format(FormatTestCase tc)
    {
        var date = TheDate;
        var f = DateTimeExtensions.GetFormatString(date, tc.Format, "PST");

        output.WriteLine(f);
        Assert.Equal(tc.Expected, date.ToString(f, CultureInfo.InvariantCulture));
    }

    public record ParseTestCase(string Name, string Format, string Value, DateTimeOffset Expected)
    {
        public override string ToString() => Name;
    }

    public class ParseTheoryData : TheoryData<ParseTestCase>
    {
        public ParseTheoryData()
        {
            AddRange(
                [
                    new(
                        "ANSIC",
                        DateTimeExtensions.Ansic,
                        "Thu Feb  4 21:00:57 2010",
                        new(2010, 2, 4, 21, 0, 57, TimeSpan.Zero)
                        ),
                    new(
                        "ANSIC Upper",
                        DateTimeExtensions.Ansic,
                        "THU FEB  4 21:00:57 2010",
                        new(2010, 2, 4, 21, 0, 57, TimeSpan.Zero)
                        ),
                    new(
                        "ANSIC Lower",
                        DateTimeExtensions.Ansic,
                        "thu feb  4 21:00:57 2010",
                        new(2010, 2, 4, 21, 0, 57, TimeSpan.Zero)
                        ),
                    new(
                        "ANSIC WS",
                        DateTimeExtensions.Ansic,
                        "Thu      Feb     4     21:00:57     2010",
                        new(2010, 2, 4, 21, 0, 57, 0, TimeSpan.Zero)
                        ),
                    new(
                        "ANSIC F",
                        DateTimeExtensions.Ansic,
                        "Thu Feb  4 21:00:57.012 2010",
                        new(2010, 2, 4, 21, 0, 57, 12, TimeSpan.Zero)
                        ),
                    new(
                        "UnixDate",
                        DateTimeExtensions.UnixDate,
                        "Thu Feb  4 21:00:57 PST 2010",
                        new(2010, 2, 4, 21, 0, 57, 0, TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("PST").BaseUtcOffset)
                        ),
                    new(
                        "UnixDate F",
                        DateTimeExtensions.UnixDate,
                        "Thu Feb  4 21:00:57.012 PST 2010",
                        new(2010, 2, 4, 21, 0, 57, 12, TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("PST").BaseUtcOffset)
                        ),
                    new(
                        "RFC3339",
                        DateTimeExtensions.Rfc3339,
                        "2010-02-04T21:00:57-08:00",
                        new(2010, 2, 4, 21, 0, 57, 0, TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("PST").BaseUtcOffset)
                        ),
                    new(
                        "RFC3339Nano",
                        DateTimeExtensions.Rfc3339Nano,
                        "2010-02-04T21:00:57.012345678-08:00",
                        new DateTimeOffset(2010, 2, 4, 21, 0, 57, 0, TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("PST").BaseUtcOffset)
                            .AddNs(012345678)
                        ),
                    new(
                        "RubyDate",
                        DateTimeExtensions.RubyDate,
                        "Thu Feb 04 21:00:57 -0800 2010",
                        new(2010, 2, 4, 21, 0, 57, 0, TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("PST").BaseUtcOffset)
                        ),
                    new(
                        "RubyDate F",
                        DateTimeExtensions.RubyDate,
                        "Thu Feb 04 21:00:57.012 -0800 2010",
                        new(2010, 2, 4, 21, 0, 57, 12, TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("PST").BaseUtcOffset)
                        ),
                    new(
                        "RFC1123 PST",
                        DateTimeExtensions.Rfc1123,
                        "Thu, 04 Feb 2010 21:00:57 PST",
                        new(2010, 2, 4, 21, 0, 57, 0, TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("PST").BaseUtcOffset)
                        ),
                    new(
                        "RFC1123 PDT",
                        DateTimeExtensions.Rfc1123,
                        "Thu, 04 Feb 2010 22:00:57 America/Los_Angeles",
                        new(2010, 2, 4, 22, 0, 57, 0, TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("America/Los_Angeles").BaseUtcOffset)
                        ),
                    new(
                        "RFC1123 PST F",
                        DateTimeExtensions.Rfc1123,
                        "Thu, 04 Feb 2010 21:00:57 PST",
                        new(2010, 2, 4, 21, 0, 57, 0, TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("PST").BaseUtcOffset)
                        ),
                    new(
                        "RFC850",
                        DateTimeExtensions.Rfc850,
                        "Thursday, 04-Feb-10 21:00:57 PST",
                        new(2010, 2, 4, 21, 0, 57, 0, TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("PST").BaseUtcOffset)
                        ),
                    new(
                        "RFC850 F",
                        DateTimeExtensions.Rfc850,
                        "Thursday, 04-Feb-10 21:00:57.012 PST",
                        new(2010, 2, 4, 21, 0, 57, 12, TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("PST").BaseUtcOffset)
                        ),
                    new(
                        "RFC822",
                        DateTimeExtensions.Rfc822,
                        "04 Feb 10 21:00 PST",
                        new(2010, 2, 4, 21, 0, 0, 0, TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("PST").BaseUtcOffset)
                        ),
                    new(
                        "RFC822Z",
                        DateTimeExtensions.Rfc822Z,
                        "04 Feb 10 21:00 -0800",
                        new(2010, 2, 4, 21, 0, 0, 0, TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("PST").BaseUtcOffset)
                        ),
                    new(
                        "RFC1123Z",
                        DateTimeExtensions.Rfc1123Z,
                        "Thu, 04 Feb 2010 21:00:57 -0800",
                        new(2010, 2, 4, 21, 0, 57, 0, TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("PST").BaseUtcOffset)
                        ),
                    new(
                        "RFC1123Z F",
                        DateTimeExtensions.Rfc1123Z,
                        "Thu, 04 Feb 2010 21:00:57.012 -0800",
                        new(2010, 2, 4, 21, 0, 57, 12, TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("PST").BaseUtcOffset)
                        ),
                    new(
                        "Custom",
                        "2006-01-02 15:04:05",
                        "2006-01-02 15:04:05",
                        new(2006, 1, 2, 15, 4, 5, 0, TimeSpan.Zero)
                        ),
                    new(
                        "Custom PST",
                        "2006-01-02 15:04:05-07",
                        "2006-01-02 15:04:05-08",
                        new(2006, 1, 2, 15, 4, 5, 0, TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("PST").BaseUtcOffset)
                        ),
                    new(
                        "Janet",
                        "Hi Janet, the Month is January: Jan _2 15:04:05 2006",
                        "Hi Janet, the Month is February: Feb  4 21:00:57 2010",
                        new(2010, 2, 4, 21, 0, 57, 0, TimeSpan.Zero)
                        ),
                    new(
                        "Janet X",
                        "Hi Janet, the Month is January: Jan _2 15:04:05 2006 MST",
                        "Hi Janet, the Month is February: Feb  4 21:00:57 2010 PST",
                        new(2010, 2, 4, 21, 0, 57, 0, TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr("PST").BaseUtcOffset)
                        ),

                    // Fractional seconds.
                    new(
                        "millisecond:: dot separator",
                        "Mon Jan _2 15:04:05.000 2006",
                        "Thu Feb  4 21:00:57.012 2010",
                        new(2010, 2, 4, 21, 0, 57, 12, TimeSpan.Zero)
                        ),
                    new(
                        "microsecond:: dot separator",
                        "Mon Jan _2 15:04:05.000000 2006",
                        "Thu Feb  4 21:00:57.012345 2010",
                        new DateTimeOffset(2010, 2, 4, 21, 0, 57, 0, TimeSpan.Zero).AddMicroseconds(12345)
                        ),
                    new(
                        "nanosecond:: dot separator",
                        "Mon Jan _2 15:04:05.000000000 2006",
                        "Thu Feb  4 21:00:57.012345678 2010",
                        new DateTimeOffset(2010, 2, 4, 21, 0, 57, 0, TimeSpan.Zero).AddNs(12345678)
                        ),
                    new(
                        "millisecond:: coma separator",
                        "Mon Jan _2 15:04:05,000 2006",
                        "Thu Feb  4 21:00:57.012 2010",
                        new(2010, 2, 4, 21, 0, 57, 12, TimeSpan.Zero)
                        ),
                    new(
                        "microsecond:: coma separator",
                        "Mon Jan _2 15:04:05,000000 2006",
                        "Thu Feb  4 21:00:57.012345 2010",
                        new DateTimeOffset(2010, 2, 4, 21, 0, 57, 0, TimeSpan.Zero).AddMicroseconds(12345)
                        ),
                    new(
                        "nanosecond:: coma separator",
                        "Mon Jan _2 15:04:05,000000000 2006",
                        "Thu Feb  4 21:00:57.012345678 2010",
                        new DateTimeOffset(2010, 2, 4, 21, 0, 57, 0, TimeSpan.Zero).AddNs(12345678)
                        ),

                    // Leading zeros in other places should not be taken as fractional seconds.
                    new(
                        "zero1",
                        "2006.01.02.15.04.05.0",
                        "2010.02.04.21.00.57.0",
                        new DateTimeOffset(2010, 2, 4, 21, 0, 57, 0, TimeSpan.Zero)
                        ),
                    new(
                        "zero2",
                        "2006.01.02.15.04.05.00",
                        "2010.02.04.21.00.57.01",
                        new DateTimeOffset(2010, 2, 4, 21, 0, 57, 10, TimeSpan.Zero)
                        ),
                    new(
                        "GMT-8",
                        DateTimeExtensions.UnixDate,
                        "Fri Feb  5 05:00:57 GMT-8 2010",
                        new DateTimeOffset(2010, 2, 5, 5, 0, 57, 0, TimeSpan.FromHours(-8))
                        ),
                    new(
                        "Custom 1",
                        "Monday, 06.January.02 15:04:05.999 MST",
                        "Thursday, 10.February.04 05:00:57.012 PST",
                        new DateTimeOffset(2010, 2, 4, 5, 0, 57, 12, TimeSpan.FromHours(-8))
                        ),

                    // Day of year.
                    new(
                        "Day of year 1",
                        "2006-01-02 002 15:04:05",
                        "2010-02-04 035 21:00:57",
                        new DateTimeOffset(2010, 2, 4, 21, 0, 57, 0, TimeSpan.Zero)
                        ),
                    new(
                        "Day of year 2",
                        "2006-01 002 15:04:05",
                        "2010-02 035 21:00:57",
                        new DateTimeOffset(2010, 2, 4, 21, 0, 57, 0, TimeSpan.Zero)
                        ),
                    new(
                        "Day of year 3",
                        "2006-002 15:04:05",
                        "2010-035 21:00:57",
                        new DateTimeOffset(2010, 2, 4, 21, 0, 57, 0, TimeSpan.Zero)
                        ),
                    new(
                        "Day of year 4",
                        "200600201 15:04:05",
                        "201003502 21:00:57",
                        new DateTimeOffset(2010, 2, 4, 21, 0, 57, 0, TimeSpan.Zero)
                        ),
                    new(
                        "Day of year 5",
                        "200600202 15:04:05",
                        "201003504 21:00:57",
                        new DateTimeOffset(2010, 2, 4, 21, 0, 57, 0, TimeSpan.Zero)
                        ),

                    // Time.
                    // new(
                    //     "Time 1",
                    //     "15:04:05",
                    //     "21:00:57",
                    //     new DateTimeOffset(1, 1, 1, 21, 0, 57, 0, TimeSpan.Zero)
                    //     ),
                    // new(
                    //     "Time 2",
                    //     "03:04:05PM",
                    //     "09:00:57PM",
                    //     new DateTimeOffset(1, 1, 1, 21, 0, 57, 0, TimeSpan.Zero)
                    //     ),
                ]
                );
        }
    }

    // [Fact]
    // public void ParseTest()
    // {
    //     ParseTestCase tc = new(
    //         "Time 1",
    //         "15:04:05",
    //         "21:00:57",
    //         new DateTimeOffset(1, 1, 1, 21, 0, 57, 0, TimeSpan.Zero)
    //         );
    //
    //     var result = DateTimeExtensions.TryParseNs(tc.Value, tc.Format, out var date);
    //
    //     Assert.True(result);
    //     Assert.Equal(tc.Expected, date);
    // }

    [Theory]
    [ClassData(typeof(ParseTheoryData))]
    public void Parse(ParseTestCase tc)
    {
        var result = DateTimeExtensions.TryParseNs(tc.Value, tc.Format, out var date);

        Assert.True(result);
        Assert.Equal(tc.Expected, date);
    }

    public static IEnumerable<object[]> TimeZoneAbbrCases()
    {
        foreach (var abbr in TimeZoneInfoExtensions.ZoneAbbreviations)
            yield return [abbr.Key];
    }

    [Theory]
    [MemberData(nameof(TimeZoneAbbrCases))]
    public void TimeZoneAbbr(string abbr)
    {
        var tz = TimeZoneInfoExtensions.FindSystemTimeZoneByIdOrAbbr(abbr);
        Assert.Equal(abbr, tz.Id);
    }
}