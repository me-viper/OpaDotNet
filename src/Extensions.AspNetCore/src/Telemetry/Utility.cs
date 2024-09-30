using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OpaDotNet.Extensions.AspNetCore.Telemetry;

internal static class Utility
{
    public const string Name = "OpaDotNet.Extensions.AspNetCore";

    public static readonly ActivitySource OpaActivitySource = new(Name, "2.0.0");

    public static readonly Meter OpaMeter = new(Name, "2.0.0");

    public const int PolicyEvaluatingEventId = 1;

    public const int PolicyAllowedEventId = 2;

    public const int PolicyDeniedEventId = 3;

    public const int PolicyFailedEventId = 4;

    public const int BundleCompiling = 5;

    public const int BundleCompilationSucceeded = 6;

    public const int BundleRecompilationSucceeded = 7;

    public const int BundleCompilationFailed = 8;

    public const int BundleCompilationHasChanges = 9;

    public const int BundleCompilationNoChanges = 10;

    public const int EvaluatorCreated = 11;

    public const int EvaluatorReleased = 12;

    public const int ServiceStopped = 13;

    public const int PolicySourceWatchStarted = 14;

    public const int PolicySourceWatchStopped = 15;

    public const int EvaluatorPoolResetting = 16;

    public const int EvaluatorPoolNotDisposable = 17;

    public const int EvaluatorPoolDisposing = 18;
}