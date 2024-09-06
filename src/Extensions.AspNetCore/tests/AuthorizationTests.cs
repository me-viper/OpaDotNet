using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Interop;
using OpaDotNet.Extensions.AspNetCore.Tests.Common;
using OpaDotNet.InternalTesting;
using OpaDotNet.Wasm;

using Xunit.Abstractions;

namespace OpaDotNet.Extensions.AspNetCore.Tests;

public class AuthorizationTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("Bad", HttpStatusCode.Forbidden)]
    [InlineData("Valid", HttpStatusCode.OK)]
    public async Task CustomAuthenticationScheme(string targetScheme, HttpStatusCode expected)
    {
        var server = await Setup(targetScheme);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{server.BaseAddress}attr/valid");

        var transaction = new Transaction
        {
            Request = request,
            Response = await server.CreateClient().SendAsync(request),
        };

        transaction.ResponseText = await transaction.Response.Content.ReadAsStringAsync();

        Assert.NotNull(transaction.Response);
        Assert.Equal(expected, transaction.Response.StatusCode);
    }

    private async Task<TestServer> Setup(string targetScheme)
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

        var factory = new TestEvaluatorFactoryProvider(new OpaBundleEvaluatorFactory(policy, opts, null));

        var builder = new WebHostBuilder()
            .ConfigureServices(
                builder =>
                {
                    builder.AddRouting();

                    builder.AddLogging(p => p.AddXunit(output).AddFilter(pp => pp > LogLevel.Trace));

                    builder.AddOpaAuthorization(
                        cfg =>
                        {
                            cfg.AddPolicySource(_ => factory);
                            cfg.AddConfiguration(
                                pp =>
                                {
                                    pp.AllowedHeaders.Add(".*");
                                    pp.IncludeClaimsInHttpRequest = true;
                                    pp.EngineOptions = opts;
                                    pp.AuthenticationSchemes = [targetScheme];
                                }
                                );
                        }
                        );

                    builder.AddAuthentication("Bad")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationSchemeHandler>("Valid", null)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationSchemeHandler>("Bad", null);

                    builder.AddAuthorization();
                }
                )
            .Configure(
                app =>
                {
                    app.UseRouting();

                    app.UseAuthentication();
                    app.UseAuthorization();

                    app.UseEndpoints(
                        p =>
                        {
                            p.MapGet(
                                "/attr/valid",
                                [Authorize("Opa/az/auth_scheme")]() => Results.Ok()
                                );
                        }
                        );
                }
                );

        return new TestServer(builder);
    }
}