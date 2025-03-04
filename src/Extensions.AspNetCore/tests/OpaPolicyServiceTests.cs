using System.Text.Json;

using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpaDotNet.Extensions.AspNetCore.Telemetry;
using OpaDotNet.Extensions.AspNetCore.Tests.Common;
using OpaDotNet.InternalTesting;
using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore.Tests;

//[Collection("Sequential")]
public class OpaPolicyServiceTests(ITestOutputHelper output)
{
    private readonly ILoggerFactory _loggerFactory = new LoggerFactory([new XunitLoggerProvider(output)]);

    [Fact]
    public async Task Recompilation()
    {
        const int maxEvaluatorsRetained = 5;
        const int maxEvaluators = 10;

        var opts = new OpaAuthorizationOptions
        {
            PolicyBundlePath = "./Policy",
            Compiler = new() { ForceBundleWriter = true },
            MaximumEvaluatorsRetained = maxEvaluatorsRetained,
            MaximumEvaluators = maxEvaluators,
            EngineOptions = new()
            {
                SerializationOptions = new()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                },
            },
        };

        var authOptions = TestOptionsMonitor.Create(opts);
        var ric = new TestingCompiler();

        using var compiler = new FileSystemPolicySource(
            new BundleCompiler(ric, authOptions, []),
            authOptions,
#pragma warning disable CA2000
            new MutableOpaEvaluatorFactory(),
#pragma warning restore CA2000,
            _loggerFactory
            );

        await compiler.StartAsync(CancellationToken.None);

        using var pooledService = new PooledOpaPolicyService(
            compiler,
            authOptions.Option(),
            new OpaEvaluatorPoolProvider(),
            _loggerFactory.CreateLogger<PooledOpaPolicyService>()
            );

        var service = new CountingEvaluator(pooledService);
        var options = new ParallelOptions { MaxDegreeOfParallelism = 4 };

        await Parallel.ForEachAsync(
            Enumerable.Range(0, 1000),
            options,
            async (i, ct) =>
            {
                if (i % 10 == 0)
                {
                    output.WriteLine($"Evaluator instances: {service.Instances}");
                    await compiler.CompileBundle(true, ct);
                }

                var result = await service.EvaluatePredicate<object?>(null, "parallel/do", CancellationToken.None);
                Assert.True(result);
            }
            );
    }

    [Fact(Skip = "Flaky")]
    public async Task PoolSize()
    {
        const int maxEvaluators = 5;

        var opts = new OptionsWrapper<OpaAuthorizationOptions>(
            new()
            {
                PolicyBundlePath = "./Policy",
                MaximumEvaluatorsRetained = maxEvaluators,
                EngineOptions = new()
                {
                    SerializationOptions = new()
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    },
                },
            }
            );

        var authOptions = TestOptionsMonitor.Create<OpaAuthorizationOptions>(opts);
        var ric = new TestingCompiler();

        using var compiler = new FileSystemPolicySource(
            new BundleCompiler(ric, authOptions, []),
            authOptions,
#pragma warning disable CA2000
            new MutableOpaEvaluatorFactory(),
#pragma warning restore CA2000,
            _loggerFactory
            );

        using var collector = new MetricCollector<long>(Utility.OpaMeter, "opadotnet_evaluator_instances");
        collector.RecordObservableInstruments();

        await compiler.StartAsync(CancellationToken.None);

        using var pooledService = new PooledOpaPolicyService(
            compiler,
            opts,
            new OpaEvaluatorPoolProvider(),
            _loggerFactory.CreateLogger<PooledOpaPolicyService>()
            );

        var service = new CountingEvaluator(pooledService);

        await Parallel.ForEachAsync(
            Enumerable.Range(0, 1000),
            async (_, _) =>
            {
                output.WriteLine($"Evaluator instances: {OpaEventSource.EvaluatorInstances}");

                var result = await service.EvaluatePredicate<object?>(null, "parallel/do", CancellationToken.None);
                Assert.True(result);
            }
            );

        Assert.Equal(0, service.Instances);
        Assert.Equal(maxEvaluators, OpaEventSource.EvaluatorInstances);
    }

    private class CountingEvaluator(IOpaPolicyService inner) : IOpaPolicyService
    {
        public int Instances;

        public ValueTask<bool> EvaluatePredicate<TInput>(TInput input, string entrypoint, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Instances);
            var result = inner.EvaluatePredicate(input, entrypoint, cancellationToken);
            Interlocked.Decrement(ref Instances);

            return result;
        }

        public ValueTask<TOutput> Evaluate<TInput, TOutput>(TInput input, string entrypoint, CancellationToken cancellationToken)
            where TOutput : notnull => inner.Evaluate<TInput, TOutput>(input, entrypoint, cancellationToken);

        public ValueTask<string> EvaluateRaw(ReadOnlyMemory<char> inputJson, string entrypoint, CancellationToken cancellationToken)
            => inner.EvaluateRaw(inputJson, entrypoint, cancellationToken);
    }
}