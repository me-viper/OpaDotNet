using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Extensions.AspNetCore.Tests.Common;
using OpaDotNet.InternalTesting;
using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore.Tests;

public class ConfigurationPolicySourceTests(ITestOutputHelper output)
{
    private readonly ILoggerFactory _loggerFactory = new LoggerFactory([new XunitLoggerProvider(output)]);

    private record UserPolicyInput([UsedImplicitly] string User);

    [Fact]
    public async Task NoPolicies()
    {
        var policyOptions = new OpaPolicyOptions();
        var optionsMonitor = new PolicyOptionsMonitor(policyOptions);
        var authOptions = TestOptionsMonitor.Create(new OpaAuthorizationOptions());

        using var compiler = new ConfigurationPolicySource(
            new BundleCompiler(
                new TestingCompiler(_loggerFactory),
                authOptions,
                []
                ),
            authOptions,
            optionsMonitor,
#pragma warning disable CA2000
            new MutableOpaEvaluatorFactory(),
#pragma warning restore CA2000
            _loggerFactory
            );

        await Assert.ThrowsAsync<RegoCompilationException>(() => compiler.StartAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData(2, "p1/t1/allow")]
    [InlineData(3, "p2/allow")]
    [InlineData(4, "p2/allow2")]
    public async Task Configuration(int data, string entrypoint)
    {
        var opts = new OpaAuthorizationOptions
        {
            Compiler = new() { Debug = true },
            EngineOptions = new WasmPolicyEngineOptions
            {
                SerializationOptions = new()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                },
            },
        };

        var policyOptions = new OpaPolicyOptions
        {
            {
                "p1",
                new()
                {
                    Package = "p1/t1",
                    DataJson = """{ "t": 2 }""",
                    Source = """
                        package p1.t1
                        import future.keywords.if
                        # METADATA
                        # entrypoint: true
                        allow if { input.t == data.p1.t1.t }
                        """,
                }
            },
            {
                "p2",
                new()
                {
                    DataYaml = "t: 3",
                    Source = """
                        package p2
                        import future.keywords.if
                        # METADATA
                        # entrypoint: true
                        allow if { input.t == data.t }
                        """,
                }
            },
            {
                "p3",
                new()
                {
                    DataYaml = "t1: 4",
                    Source = """
                        package p2
                        import future.keywords.if
                        # METADATA
                        # entrypoint: true
                        allow2 if { input.t == data.t1 }
                        """,
                }
            },
        };

        var optionsMonitor = new PolicyOptionsMonitor(policyOptions);

        var authOptions = TestOptionsMonitor.Create(opts);
        var ric = new TestingCompiler(_loggerFactory);

        using var compiler = new ConfigurationPolicySource(
            new BundleCompiler(ric, authOptions, []),
            authOptions,
            optionsMonitor,
#pragma warning disable CA2000
            new MutableOpaEvaluatorFactory(),
#pragma warning restore CA2000
            _loggerFactory
            );

        await compiler.StartAsync(CancellationToken.None);

        using var evaluator = compiler.CreateEvaluator();
        var result = evaluator.EvaluatePredicate(new { t = data }, entrypoint);

        Assert.True(result.Result);
    }

    [Fact]
    public async Task WatchChanges()
    {
        var opts = new OpaAuthorizationOptions
        {
            MonitoringInterval = TimeSpan.FromSeconds(3),

            //PolicyBundlePath = "./Watch",
            EngineOptions = new WasmPolicyEngineOptions
            {
                SerializationOptions = new()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                },
            },
        };

        var policyOptions = new OpaPolicyOptions
        {
            {
                "p1",
                new()
                {
                    Package = "watch",
                    Source = Policy(0),
                    DataJson = "{}",
                    DataYaml = "",
                }
            },
        };

        var optionsMonitor = new PolicyOptionsMonitor(policyOptions);

        var authOptions = TestOptionsMonitor.Create(opts);
        var ric = new TestingCompiler(_loggerFactory);

        using var compiler = new ConfigurationPolicySource(
            new BundleCompiler(ric, TestOptionsMonitor.Create(opts), []),
            authOptions,
            optionsMonitor,
#pragma warning disable CA2000
            new MutableOpaEvaluatorFactory(),
#pragma warning restore CA2000
            _loggerFactory
            );

        await compiler.StartAsync(CancellationToken.None);

        for (var i = 0; i < 3; i++)
        {
            var eval = compiler.CreateEvaluator();
            var result = eval.EvaluatePredicate(new UserPolicyInput($"u{i}"));

            output.WriteLine($"Checking: u{i}");
            Assert.True(result.Result);

            var newOpts = new OpaPolicyOptions { { "p1", new() { Source = Policy(i + 1) } } };
            optionsMonitor.Change(newOpts);

            await Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }

        await compiler.StopAsync(CancellationToken.None);
    }

    private static string Policy(int i)
    {
        return $$"""
            package watch
            import future.keywords.if

            # METADATA
            # entrypoint: true
            user if {
                input.user == "u{{i}}"
            }
            """;
    }

    private class PolicyOptionsMonitor : IOptionsMonitor<OpaPolicyOptions>
    {
        private Action<OpaPolicyOptions, string?>? _listener;

        public OpaPolicyOptions CurrentValue { get; private set; }

        public PolicyOptionsMonitor(OpaPolicyOptions opts)
        {
            CurrentValue = opts;
        }

        public OpaPolicyOptions Get(string? name)
        {
            return CurrentValue;
        }

        public void Change(OpaPolicyOptions opts)
        {
            CurrentValue = opts;
            _listener?.Invoke(CurrentValue, null);
        }

        public IDisposable? OnChange(Action<OpaPolicyOptions, string?> listener)
        {
            _listener = listener;
            return null;
        }
    }
}