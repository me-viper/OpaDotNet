using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Interop;
using OpaDotNet.Extensions.AspNetCore.Tests.Common;
using OpaDotNet.InternalTesting;
using OpaDotNet.Wasm;

using Xunit.Abstractions;

namespace OpaDotNet.Extensions.AspNetCore.Tests;

public class HttpRequestPolicyInputTests(ITestOutputHelper output) : IAsyncLifetime
{
    private readonly ILoggerFactory _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });

    private IOpaPolicySource _policySource = default!;

    public async Task InitializeAsync()
    {
        var compiler = new RegoInteropCompiler();
        var policy = await compiler.CompileBundleAsync("./Policy", new());

        var opts = new WasmPolicyEngineOptions
        {
            SerializationOptions = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            },
        };

        _policySource = new TestEvaluatorFactoryProvider(
            new OpaBundleEvaluatorFactory(policy, opts, () => new CoreImportsAbi(_loggerFactory.CreateLogger<CoreImportsAbi>()))
            );
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
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