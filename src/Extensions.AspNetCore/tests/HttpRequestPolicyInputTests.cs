using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.InternalTesting;
using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore.Tests;

public class HttpRequestPolicyInputTests(ITestOutputHelper output) : IAsyncLifetime
{
    private readonly ILoggerFactory _loggerFactory = new LoggerFactory([new XunitLoggerProvider(output)]);

    private IOpaPolicySource _policySource = default!;

    public async ValueTask InitializeAsync()
    {
        var compiler = new TestingCompiler(_loggerFactory);
        var policy = await compiler.CompileBundleAsync("./Policy", new());

        var opts = new WasmPolicyEngineOptions
        {
            SerializationOptions = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            },
        };

        _policySource = new TestPolicySource(
#pragma warning disable CA2000
            new OpaBundleEvaluatorFactory(
                policy,
                opts
                )
#pragma warning restore CA2000
            );
    }

    public ValueTask DisposeAsync()
    {
        _policySource.Dispose();
        return ValueTask.CompletedTask;
    }

    [Theory]
    [InlineData("""["a", "b", "c", "test"]""", "string")]
    [InlineData("""["a", "b", "c", "test"]""", JsonClaimValueTypes.JsonArray)]
    public void ArrayClaims(string value, string type)
    {
        var context = new DefaultHttpContext();
        var claims = new Claim[] { new("role", value, type) };

        var input = new HttpRequestPolicyInput(context.Request, new HashSet<string>(), claims);

        var evaluator = _policySource.CreateEvaluator();
        var result = evaluator.EvaluatePredicate(input, "http_in/claim_value_array");

        Assert.True(result.Result);
    }
}