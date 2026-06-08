using System.Collections.ObjectModel;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
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

    [Fact]
    public void ClientCert()
    {
        var cert = """
        -----BEGIN CERTIFICATE-----
        MIICrTCCAZUCFDoVrgQzTrSzmzr1AtY3xAzPChflMA0GCSqGSIb3DQEBCwUAMBEx
        DzANBgNVBAMMBm15LW9yZzAeFw0yNjA2MDgwNTU1NTVaFw0yNzA2MDgwNTU1NTVa
        MBUxEzARBgNVBAMMCmNsaWVudGhvc3QwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAw
        ggEKAoIBAQDGTUk+YN2rqYmDt9yspXUlHtL+++/843ILbE4Apk+YhI204kMMBsZ1
        eCG7YHYVjXnUDTD4fznUV88+kpNsjCzdZ11AXJaDY4JKX5BLG44Xv/MZy8Atw6n2
        0UJtlwAIVV2jV6BDKoSonv91XqvhtwJtO2XSMI8c74+4WyEbQaaRyy+m/wsk/2i5
        tQKzTEnXEbZRuxfZrZdNtdTOCVH9roAx55UWnUm9jh/WMUolCWUVtKVSk9JH15RL
        RLyXglhmUgBIPUBXOXldbvx9UyjhUBkLTMmLm1fh0vDJvmvoEgsIAObOp7OQECsL
        2czfhBgDyUreAOOH8h8TeZ5acCUubWDZAgMBAAEwDQYJKoZIhvcNAQELBQADggEB
        ADwd9KPFdoRy3tNf6pFEECRTXoJDwl45GUTrMlUooKAEIc+SA+Fx4TqlfB+ikCsc
        C9Acm37LE2Z9dfCI//Euqo48r4Js+krDDCtP5+/+YdUrsX/bCxpToCRTdxH1XqJY
        yWHYCNCB5oiOrXOVYX7GvBbu7yCtrlXGI3vSB6sWEI9eNk670To3KFFmx2E6ZF7E
        3lSucnNb5QQdL6HEoMrwyy3mQBEcmxXinswg9jKj5N7y3dEJAQOalauknBcNAttw
        TINVwMTcgbVb1Co6SDpm8WqsQREKE2jq7kpPwIRVrUTRHg1efVFa4PpceK6/qrfA
        acbqo9gr8sFzsGtl2i8R9BI=
        -----END CERTIFICATE-----

        """;

        var key = """
        -----BEGIN ENCRYPTED PRIVATE KEY-----
        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
        -----END ENCRYPTED PRIVATE KEY-----
        """;

        var context = new DefaultHttpContext();
        context.Connection.ClientCertificate = X509Certificate2.CreateFromEncryptedPem(cert, key, "test");

        var input = new HttpRequestPolicyInput(context.Request, new HashSet<string>());

        var evaluator = _policySource.CreateEvaluator();
        var result = evaluator.EvaluatePredicate(input, "http_in/client_cert");

        Assert.True(result.Result);
    }
}