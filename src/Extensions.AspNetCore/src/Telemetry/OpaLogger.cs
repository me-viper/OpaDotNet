namespace OpaDotNet.Extensions.AspNetCore.Telemetry;

internal static partial class OpaLogger
{
    [LoggerMessage(EventId = Utility.PolicyEvaluatingEventId, Message = "Evaluating policy", Level = LogLevel.Debug)]
    public static partial void PolicyEvaluating(this ILogger logger);


    [LoggerMessage(EventId = Utility.PolicyAllowedEventId, Message = "Authorization policy succeeded", Level = LogLevel.Debug)]
    public static partial void PolicyAllowed(this ILogger logger);

    [LoggerMessage(EventId = Utility.PolicyAllowedEventId, Message = "Authorization policy denied", Level = LogLevel.Debug)]
    public static partial void PolicyDenied(this ILogger logger);

    [LoggerMessage(EventId = Utility.PolicyFailedEventId, Message = "Authorization policy failed", Level = LogLevel.Error)]
    public static partial void PolicyFailed(this ILogger logger, Exception ex);

    [LoggerMessage(EventId = Utility.BundleCompiling, Message = "Compiling...", Level = LogLevel.Debug)]
    public static partial void BundleCompiling(this ILogger logger);

    [LoggerMessage(
        EventId = Utility.BundleRecompilationSucceeded,
        Message = "Recompilation completed. Triggering notifications",
        Level = LogLevel.Debug
        )]
    public static partial void BundleRecompilationSucceeded(this ILogger logger);

    [LoggerMessage(EventId = Utility.BundleCompilationSucceeded, Message = "Compilation succeeded", Level = LogLevel.Debug)]
    public static partial void BundleCompilationSucceeded(this ILogger logger);

    [LoggerMessage(
        EventId = Utility.BundleCompilationHasChanges,
        Message = "Detected changes in policy",
        Level = LogLevel.Debug
        )]
    public static partial void BundleCompilationHasChanges(this ILogger logger);

    [LoggerMessage(
        EventId = Utility.BundleCompilationNoChanges,
        Message = "No changes in policies configuration",
        Level = LogLevel.Debug
        )]
    public static partial void BundleCompilationNoChanges(this ILogger logger);

    [LoggerMessage(EventId = Utility.BundleCompilationFailed, Message = "Bundle compilation failed", Level = LogLevel.Error)]
    public static partial void BundleCompilationFailed(this ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = Utility.PolicySourceWatchStarted,
        Message = "Watching for policy changes in {Path}",
        Level = LogLevel.Debug
        )]
    public static partial void PolicySourceWatchStarted(this ILogger logger, string path);

    [LoggerMessage(
        EventId = Utility.PolicySourceWatchStopped,
        Message = "Stopped watching for policy changes",
        Level = LogLevel.Warning
        )]
    public static partial void PolicySourceWatchStopped(this ILogger logger);

    [LoggerMessage(EventId = Utility.ServiceStopped, Message = "Stopped", Level = LogLevel.Debug)]
    public static partial void ServiceStopped(this ILogger logger);

    [LoggerMessage(
        EventId = Utility.EvaluatorPoolResetting,
        Message = "Recompiled. Resetting pool",
        Level = LogLevel.Debug
        )]
    public static partial void EvaluatorPoolResetting(this ILogger logger);

    [LoggerMessage(
        EventId = Utility.EvaluatorPoolNotDisposable,
        Message = "Pool is not disposable",
        Level = LogLevel.Warning
        )]
    public static partial void EvaluatorPoolNotDisposable(this ILogger logger);

    [LoggerMessage(
        EventId = Utility.EvaluatorPoolDisposing,
        Message = "Disposing old pool",
        Level = LogLevel.Debug
        )]
    public static partial void EvaluatorPoolDisposing(this ILogger logger);
}