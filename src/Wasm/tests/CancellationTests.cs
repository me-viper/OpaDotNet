using OpaDotNet.InternalTesting;
using OpaDotNet.Wasm.Builtins;
using OpaDotNet.Wasm.Tests.Common;

namespace OpaDotNet.Wasm.Tests;

public class CancellationTests : SdkTestBase
{
    private const string Entrypoint = "timeout/result";

    public CancellationTests(ITestOutputHelper output) : base(output)
    {
        using var capsStream = new CapsProvider().GetCapabilities();
        Memory<byte> buf = new byte[capsStream.Length];
        _ = capsStream.Read(buf.Span);

        Options = new()
        {
            CapabilitiesVersion = Utils.DefaultCapabilities,
            CapabilitiesBytes = buf,
        };
    }

    private static WasmPolicyEngineOptions EngineOptions(long timeout = 2)
    {
        var opts = new WasmPolicyEngineOptions
        {
            StrictBuiltinErrors = true,
            Timeout = TimeSpan.FromSeconds(timeout),
            SerializationOptions = new(JsonSerializerOptions.Default)
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            },
        };

        opts.ConfigureBuiltins(
            p =>
            {
                p.DefaultBuiltins = new NotImplementedImports();
                p.CustomBuiltins.Add(new CancellableBuiltins(NullLogger.Instance));
            }
            );

        return opts;
    }

    [Fact]
    public async Task SyncTimeout()
    {
        var src = """
            package timeout
            result := timeout.sync()
            """;

        using var engine = await Build(src, Entrypoint, EngineOptions());

        Assert.Throws<OpaBuiltinException>(() => engine.Evaluate<object, string>(new object(), Entrypoint));
    }

    [Fact]
    public async Task AsyncTimeout()
    {
        var src = """
            package timeout
            result := timeout.async()
            """;

        using var engine = await Build(src, Entrypoint, EngineOptions());

        Assert.Throws<OpaBuiltinException>(() => engine.Evaluate<object, string>(new object(), Entrypoint));
    }

    [Fact]
    public async Task SyncTimeout2()
    {
        var src = """
            package timeout
            result := timeout.sync2()
            """;

        using var engine = await Build(src, Entrypoint, EngineOptions());

        Assert.Throws<OpaBuiltinException>(() => engine.Evaluate<object, string>(new object(), Entrypoint));
    }

    [Fact]
    public async Task AsyncTimeout2()
    {
        var src = """
            package timeout
            result := timeout.async2(input.t)
            """;

        using var engine = await Build(src, Entrypoint, EngineOptions());

        Assert.Throws<OpaBuiltinException>(() => engine.Evaluate<object, string>(new { t = 3 }, Entrypoint));
    }

    [Fact]
    public async Task ResetAfterTimeout()
    {
        var src = """
            package timeout
            result := timeout.async2(input.t)
            """;

        using var engine = await Build(src, Entrypoint, EngineOptions());

        Assert.Throws<OpaBuiltinException>(() => engine.Evaluate<object, string>(new { t = 3 }, Entrypoint));

        var result = engine.Evaluate<object, string>(new { t = 1 }, Entrypoint);

        Assert.NotNull(result);
        Assert.Equal("Ok!", result.Result);
    }

    [Fact]
    public async Task SyncError()
    {
        var src = """
            package timeout
            result := timeout.sync_error()
            """;

        using var engine = await Build(src, Entrypoint, EngineOptions());

        Assert.Throws<OpaBuiltinException>(
            // ReSharper disable once AccessToDisposedClosure
            () => engine.Evaluate<object, string>(new object(), Entrypoint),
            p => p.Message == "Fail" ? null : "Unexpected exception"
            );
    }

    [Fact]
    public async Task AsyncSyncError()
    {
        var src = """
            package timeout
            result := timeout.async_error()
            """;

        using var engine = await Build(src, Entrypoint, EngineOptions());

        Assert.Throws<OpaBuiltinException>(
            // ReSharper disable once AccessToDisposedClosure
            () => engine.Evaluate<object, string>(new object(), Entrypoint),
            p => p.Message == "Fail" ? null : "Unexpected exception"
            );
    }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
file class CancellableBuiltins(ILogger logger) : IOpaCustomBuiltins
{
    private const int DefaultDelay = 5;

    public void Reset() => logger.LogDebug("Reset");

    [OpaCustomBuiltin("timeout.ok")]
    public static string Ok() => "Ok!";

    [OpaCustomBuiltin("timeout.sync")]
    public static void SyncTimeout()
    {
        var i = 0;

        while (i < DefaultDelay)
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            i++;
        }

        throw new OpaEvaluationException("You should not be here!");
    }

    [OpaCustomBuiltin("timeout.sync2")]
    public static string SyncTimeout(IOpaCustomBuiltinsContext context)
    {
        var i = 0;

        while (i < DefaultDelay)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            Thread.Sleep(TimeSpan.FromSeconds(1));
            i++;
        }

        throw new OpaEvaluationException("You should not be here!");
    }

    [OpaCustomBuiltin("timeout.async")]
    public static async Task AsyncTimeout()
    {
        await Task.Delay(TimeSpan.FromSeconds(DefaultDelay));
        throw new OpaEvaluationException("You should not be here!");
    }

    [OpaCustomBuiltin("timeout.async2")]
    public static async Task<string> AsyncTimeout(int timeout, IOpaCustomBuiltinsContext context)
    {
        await Task.Delay(TimeSpan.FromSeconds(timeout), context.CancellationToken);
        return "Ok!";
    }

    [OpaCustomBuiltin("timeout.sync_error")]
    public static void SyncError()
    {
        throw new InvalidOperationException("Fail");
    }

    [OpaCustomBuiltin("timeout.async_error")]
    public static async Task AsyncError()
    {
        throw new InvalidOperationException("Fail");
    }
}

file class CapsProvider : ICapabilitiesProvider
{
    public Stream GetCapabilities()
    {
        var caps = """
            {
              "builtins": [
                {
                  "name": "timeout.ok",
                  "decl": {
                    "type": "function",
                    "args": [],
                    "result": { "type": "string" }
                  }
                },
                {
                  "name": "timeout.sync",
                  "decl": {
                    "type": "function",
                    "args": [],
                    "result": { "type": "object" }
                  }
                },
                {
                  "name": "timeout.sync2",
                  "decl": {
                    "type": "function",
                    "args": [],
                    "result": { "type": "string" }
                  }
                },
                {
                  "name": "timeout.async",
                  "decl": {
                    "type": "function",
                    "args": [],
                    "result": { "type": "object" }
                  }
                },
                {
                  "name": "timeout.async2",
                  "decl": {
                    "type": "function",
                    "args": [ { "type": "number" } ],
                    "result": { "type": "string" }
                  }
                },
                {
                  "name": "timeout.sync_error",
                  "decl": {
                    "type": "function",
                    "args": [],
                    "result": { "type": "object" }
                  }
                },
                {
                  "name": "timeout.async_error",
                  "decl": {
                    "type": "function",
                    "args": [],
                    "result": { "type": "object" }
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