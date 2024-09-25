using System.Text.Json;

using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpaDotNet.Compilation.Interop;
using OpaDotNet.Extensions.AspNetCore.Telemetry;
using OpaDotNet.Extensions.AspNetCore.Tests.Common;
using OpaDotNet.InternalTesting;

namespace OpaDotNet.Extensions.AspNetCore.Tests;

public class OpaPolicyServiceTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    private readonly ILoggerFactory _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });

    [Fact]
    public async Task Recompilation()
    {
        const int maxEvaluators = 5;

        var opts = new OpaAuthorizationOptions
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
        };

        var authOptions = TestOptionsMonitor.Create(opts);
        var ric = new RegoInteropCompiler();

        var compiler = new FileSystemPolicySource(
            new BundleCompiler(ric, authOptions, []),
            authOptions,
            new OpaBundleEvaluatorFactoryBuilder(authOptions, new TestBuiltinsFactory(_loggerFactory)),
            _loggerFactory
            );

        await compiler.StartAsync(CancellationToken.None);

        using var service = new PooledOpaPolicyService(
            compiler,
            authOptions.Option(),
            new OpaEvaluatorPoolProvider(),
            _loggerFactory.CreateLogger<PooledOpaPolicyService>()
            );

        await Parallel.ForEachAsync(
            Enumerable.Range(0, 1000),
            async (i, ct) =>
            {
                if (i % 10 == 0)
                    await compiler.CompileBundle(true, ct);

                var result = service.EvaluatePredicate<object?>(null, "parallel/do");
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
        var ric = new RegoInteropCompiler();

        var compiler = new FileSystemPolicySource(
            new BundleCompiler(ric, authOptions, []),
            authOptions,
            new OpaBundleEvaluatorFactoryBuilder(authOptions, new TestBuiltinsFactory(_loggerFactory)),
            _loggerFactory
            );

        var collector = new MetricCollector<long>(Utility.OpaMeter, "opadotnet_evaluator_instances");
        collector.RecordObservableInstruments();

        await compiler.StartAsync(CancellationToken.None);

        using var pooledService = new PooledOpaPolicyService(
            compiler,
            opts,
            new OpaEvaluatorPoolProvider(),
            _loggerFactory.CreateLogger<PooledOpaPolicyService>()
            );

        var service = new CountingEvaluator(pooledService);

        Parallel.ForEach(
            Enumerable.Range(0, 1000),
            _ =>
            {
                var result = service.EvaluatePredicate<object?>(null, "parallel/do");
                Assert.True(result);
            }
            );

        Assert.Equal(0, service.Instances);
        Assert.Equal(maxEvaluators, OpaEventSource.EvaluatorInstances);
    }

    private class CountingEvaluator(IOpaPolicyService inner) : IOpaPolicyService
    {
        public int Instances;

        public bool EvaluatePredicate<TInput>(TInput input, string entrypoint)
        {
            Interlocked.Increment(ref Instances);
            var result = inner.EvaluatePredicate(input, entrypoint);
            Interlocked.Decrement(ref Instances);

            return result;
        }

        public TOutput Evaluate<TInput, TOutput>(TInput input, string entrypoint) where TOutput : notnull
            => inner.Evaluate<TInput, TOutput>(input, entrypoint);

        public string EvaluateRaw(ReadOnlySpan<char> inputJson, string entrypoint)
            => inner.EvaluateRaw(inputJson, entrypoint);
    }
}