namespace OpaDotNet.Wasm.GoCompat;

internal static class DateTimeExtensions
{
    public static DateTimeOffset FromEpochNs(long ns) => DateTimeOffset.UnixEpoch.AddTicks(ns / 100);

    public static DateTimeOffset FromNs(long ns) => new(ns / 100, TimeSpan.Zero);

    public static long ToEpochNs(this DateTimeOffset d) => (long)(d - DateTimeOffset.UnixEpoch).TotalNanoseconds;

    public static string Format(this DateTimeOffset d, string layout)
    {
        //var x = DateTimeOffset

        throw new NotImplementedException();
    }
}