using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.InternalTesting;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Builtins;

namespace OpaDotNet.Extensions.AspNetCore.Tests;

public class CustomBuiltinsTest(ITestOutputHelper output)
{
    [Fact]
    public async Task Works()
    {
        var services = new ServiceCollection();
        services.AddLogging(p => p.AddXunit(output).AddFilter(pp => pp > LogLevel.Trace));
        services.AddSingleton(TimeProvider.System);

        services
            .AddOptions<OpaAuthorizationOptions>()
            .Configure(
                p =>
                {
                    p.Compiler = new()
                    {
                        Debug = true,
                        RegoVersion = RegoVersion.V1,
                        CapabilitiesVersion = Utils.DefaultCapabilities,
                    };

                    p.EngineOptions = new WasmPolicyEngineOptions
                    {
                        SerializationOptions = new()
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        },
                    };
                }
                );

        services
            .AddOptions<OpaPolicyOptions>()
            .Configure(
                p =>
                {
                    p["c1"] = new()
                    {
                        Package = "c1/test",
                        Source = """
                            package c1.test
                            # METADATA
                            # entrypoint: true
                            allow if {
                                print("c1.test")
                                custom1.func(input.t)
                            }
                            """,
                    };

                    p["c2"] = new()
                    {
                        Package = "c2/test",
                        Source = """
                            package c2.test
                            # METADATA
                            # entrypoint: true
                            allow if {
                                print("c2.test")
                                custom2.func(input.t)
                            }
                            """,
                    };
                }
                );

        services.AddOpaAuthorization(
            cfg =>
            {
                cfg.AddCompiler<TestingCompiler>();
                cfg.AddPolicySource<ConfigurationPolicySource>();
                cfg.AddCustomBuiltins<CustomPrinter>();
                cfg.AddCustomBuiltins<Custom1, Custom1>();
                cfg.AddCustomBuiltins<Custom2, Custom2Caps>();
            }
            );

        var sp = services.BuildServiceProvider();

        using var compiler = sp.GetRequiredService<IOpaPolicySource>();
        await compiler.StartAsync(CancellationToken.None);

        var evaluator = compiler.CreateEvaluator();

        var t1 = evaluator.EvaluatePredicate(new { t = "test1" }, "c1/test/allow");
        Assert.True(t1.Result);

        var t2 = evaluator.EvaluatePredicate(new { t = "test2" }, "c2/test/allow");
        Assert.True(t2.Result);
    }
}

file class CustomPrinter(ILogger<CustomPrinter> logger) : IOpaCustomBuiltins, IOpaCustomPrinter
{
    public void Print(IEnumerable<string> args) => logger.LogInformation("Custom: {Log}", string.Join(" ", args));
}

file class Custom1 : IOpaCustomBuiltins, ICapabilitiesProvider
{
    [OpaCustomBuiltin("custom1.func")]
    public bool Func(string arg1) => arg1.Equals("test1", StringComparison.Ordinal);

    public Stream GetCapabilities()
    {
        var caps = """
            {
                "builtins": [
                  {
                    "name": "custom1.func",
                    "decl": {
                      "type": "function",
                      "args": [
                        {
                          "type": "string"
                        }
                      ],
                      "result": {
                        "type": "boolean"
                      }
                    }
                  }
                ]
            }
            """u8;

        var ms = new MemoryStream();
        ms.Write(caps);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}

file class Custom2 : IOpaCustomBuiltins
{
    public void Reset()
    {
    }

    [OpaCustomBuiltin("custom2.func")]
    public bool Func(string arg1) => arg1.Equals("test2", StringComparison.Ordinal);
}

file class Custom2Caps : ICapabilitiesProvider
{
    public Stream GetCapabilities()
    {
        var caps = """
            {
              "builtins": [
                {
                  "name": "custom2.func",
                  "decl": {
                    "type": "function",
                    "args": [
                      {
                        "type": "string"
                      }
                    ],
                    "result": {
                      "type": "boolean"
                    }
                  }
                }
              ]
            }
            """u8;

        var ms = new MemoryStream();
        ms.Write(caps);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}