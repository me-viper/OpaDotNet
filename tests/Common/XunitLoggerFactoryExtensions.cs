using Microsoft.Extensions.DependencyInjection;

using Xunit.Abstractions;

namespace OpaDotNet.Tests.Common;

public static class XunitLoggerFactoryExtensions
{
    public static ILoggingBuilder AddXunit(this ILoggingBuilder builder, ITestOutputHelper output)
    {
        builder.Services.AddSingleton<ILoggerProvider>(new XunitLoggerProvider(output));
        return builder;
    }

    public static ILoggingBuilder AddXunit(this ILoggingBuilder builder, ITestOutputHelper output, LogLevel minLevel)
    {
        builder.Services.AddSingleton<ILoggerProvider>(new XunitLoggerProvider(output, minLevel));
        return builder;
    }

    public static ILoggingBuilder AddXunit(this ILoggingBuilder builder, ITestOutputHelper output, LogLevel minLevel, DateTimeOffset? logStart)
    {
        builder.Services.AddSingleton<ILoggerProvider>(new XunitLoggerProvider(output, minLevel, logStart));
        return builder;
    }

    public static ILoggerFactory AddXunit(this ILoggerFactory loggerFactory, ITestOutputHelper output)
    {
        loggerFactory.AddProvider(new XunitLoggerProvider(output));
        return loggerFactory;
    }

    public static ILoggerFactory AddXunit(this ILoggerFactory loggerFactory, ITestOutputHelper output, LogLevel minLevel)
    {
        loggerFactory.AddProvider(new XunitLoggerProvider(output, minLevel));
        return loggerFactory;
    }

    public static ILoggerFactory AddXunit(this ILoggerFactory loggerFactory, ITestOutputHelper output, LogLevel minLevel, DateTimeOffset? logStart)
    {
        loggerFactory.AddProvider(new XunitLoggerProvider(output, minLevel, logStart));
        return loggerFactory;
    }
}