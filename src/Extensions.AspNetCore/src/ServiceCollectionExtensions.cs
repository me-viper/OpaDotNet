using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Builtins;

namespace OpaDotNet.Extensions.AspNetCore;

[PublicAPI]
[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IOpaAuthorizationBuilder AddJsonOptions(
        this IOpaAuthorizationBuilder builder,
        Action<JsonSerializerOptions> jsonOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(jsonOptions);

        builder.Services
            .AddOptions<OpaAuthorizationOptions>()
            .PostConfigure(
                p => { jsonOptions.Invoke(p.EngineOptions.SerializationOptions); }
                );

        return builder;
    }

    public static IOpaAuthorizationBuilder AddConfiguration(
        this IOpaAuthorizationBuilder builder,
        Action<OpaAuthorizationOptions> configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.Services.Configure(configuration);

        return builder;
    }

    public static IOpaAuthorizationBuilder AddConfiguration(
        this IOpaAuthorizationBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.Services.Configure<OpaAuthorizationOptions>(configuration);

        return builder;
    }

    public static IOpaAuthorizationBuilder AddCustomBuiltins<T>(this IOpaAuthorizationBuilder builder)
        where T : class, IOpaCustomBuiltins
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddTransient<IOpaCustomBuiltins, T>();

        return builder;
    }

    public static IOpaAuthorizationBuilder AddCustomBuiltins<TBuiltins, TCapabilities>(this IOpaAuthorizationBuilder builder)
        where TBuiltins : class, IOpaCustomBuiltins
        where TCapabilities : class, ICapabilitiesProvider
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddTransient<IOpaCustomBuiltins, TBuiltins>();
        builder.Services.AddTransient<ICapabilitiesProvider, TCapabilities>();

        return builder;
    }

    public static IOpaAuthorizationBuilder AddCompiler<T>(
        this IOpaAuthorizationBuilder builder)
        where T : class, IRegoCompiler
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<IRegoCompiler, T>();

        return builder;
    }

    public static IOpaAuthorizationBuilder AddCompiler<T>(
        this IOpaAuthorizationBuilder builder,
        Func<IServiceProvider, T> buildCompiler)
        where T : class, IRegoCompiler
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddSingleton<IRegoCompiler>(buildCompiler);

        return builder;
    }

    public static IOpaAuthorizationBuilder AddConfigurationPolicySource(
        this IOpaAuthorizationBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.Services.Configure<OpaPolicyOptions>(configuration);
        builder.AddPolicySource<ConfigurationPolicySource>();

        return builder;
    }

    public static IOpaAuthorizationBuilder AddConfigurationPolicySource(
        this IOpaAuthorizationBuilder builder,
        Action<OpaPolicyOptions> configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        builder.Services.Configure(configuration);
        builder.AddPolicySource<ConfigurationPolicySource>();

        return builder;
    }

    public static IOpaAuthorizationBuilder AddFileSystemPolicySource(this IOpaAuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddPolicySource<FileSystemPolicySource>();

        return builder;
    }

    public static IOpaAuthorizationBuilder AddPolicySource<T>(
        this IOpaAuthorizationBuilder builder,
        Func<IServiceProvider, T>? buildPolicySource = null) where T : class, IOpaPolicySource
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (buildPolicySource == null)
            builder.Services.TryAddSingleton<IOpaPolicySource, T>();
        else
            builder.Services.TryAddSingleton<IOpaPolicySource>(buildPolicySource);

        builder.Services.AddHostedService(p => p.GetRequiredService<IOpaPolicySource>());

        return builder;
    }

    public static IServiceCollection AddOpaAuthorization(
        this IServiceCollection services,
        Action<IOpaAuthorizationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new OpaAuthorizationBuilder(services);
        configure(builder);

        services.AddOpaAuthorization(builder);

        services.TryAddSingleton(NopCompiler.Instance);
        services.TryAddSingleton<IBundleCompiler, BundleCompiler>();
        services.TryAddTransient<IOpaImportsAbi, CoreImportsAbi>();
        services.AddTransient<IConfigureOptions<OpaAuthorizationOptions>, ConfigureOpaAuthorizationOptions>();
        services.AddTransient<IMutableOpaEvaluatorFactory, MutableOpaEvaluatorFactory>();

        return services;
    }

    private static IServiceCollection AddOpaAuthorization(this IServiceCollection services, OpaAuthorizationBuilder builder)
    {
        services.AddOptions();
        services.TryAddSingleton<OpaEvaluatorPoolProvider>();
        services.TryAddSingleton<IAuthorizationPolicyProvider>(
            p => new OpaPolicyProvider(
                p.GetRequiredService<IOptions<AuthorizationOptions>>(),
                p.GetRequiredService<IOptions<OpaAuthorizationOptions>>()
                )
            );
        services.TryAddSingleton<IAuthorizationHandler, OpaPolicyHandler>();
        services.TryAddSingleton<IOpaPolicyService, PooledOpaPolicyService>();

        return services;
    }
}