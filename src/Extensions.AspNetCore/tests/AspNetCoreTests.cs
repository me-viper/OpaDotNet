using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Extensions.AspNetCore.Tests.Common;
using OpaDotNet.InternalTesting;
using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore.Tests;

//[Collection("Sequential")]
public class AspNetCoreTests(ITestOutputHelper output) : IAsyncLifetime
{
    private readonly DirectoryInfo _outputDirectory = Utils.CreateTempDirectory(AppContext.BaseDirectory);

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _outputDirectory.Delete(true);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task HttpRequestInput()
    {
        using var server = CreateServer(
            output,
            handler: async (context, _) =>
            {
                var request = context.Request;
                var azs = context.RequestServices.GetRequiredService<IAuthorizationService>();
                var result = await azs.AuthorizeAsync(context.User, request, "Opa/az/rq_path");

                if (!result.Succeeded)
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }
            );

        var request = new HttpRequestMessage(HttpMethod.Get, $"{server.BaseAddress}az/request");

        var transaction = new Transaction
        {
            Request = request,
            Response = await server.CreateClient().SendAsync(request),
        };

        transaction.ResponseText = await transaction.Response.Content.ReadAsStringAsync();

        Assert.NotNull(transaction.Response);
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
    }

    [Theory]
    [InlineData("u1", HttpStatusCode.OK)]
    [InlineData("wrong", HttpStatusCode.Forbidden)]
    public async Task Simple(string user, HttpStatusCode expected)
    {
        using var server = CreateServer(
            output,
            handler: async (context, _) =>
            {
                var azs = context.RequestServices.GetRequiredService<IAuthorizationService>();

                var result = await azs.AuthorizeAsync(context.User, new UserPolicyInput(user), "Opa/az/user");

                if (!result.Succeeded)
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            },
            configureServices: p => p.AddSingleton<IAuthorizationHandler, OpaPolicyHandler<UserPolicyInput>>()
            );

        var request = new HttpRequestMessage(HttpMethod.Get, $"{server.BaseAddress}");

        var transaction = new Transaction
        {
            Request = request,
            Response = await server.CreateClient().SendAsync(request),
        };

        transaction.ResponseText = await transaction.Response.Content.ReadAsStringAsync();

        Assert.NotNull(transaction.Response);
        Assert.Equal(expected, transaction.Response.StatusCode);
    }

    [Theory]
    [InlineData("u1", HttpStatusCode.OK)]
    [InlineData("wrong", HttpStatusCode.Forbidden)]
    public async Task CompositeAuthorizationPolicyProvider(string user, HttpStatusCode expected)
    {
        using var server = CreateServer(
            output,
            handler: async (context, _) =>
            {
                var azs = context.RequestServices.GetRequiredService<IAuthorizationService>();

                var policy1 = await azs.AuthorizeAsync(context.User, new UserPolicyInput(user), "Opa/az/user");
                var policy2 = await azs.AuthorizeAsync(context.User, null, "some");

                if (!policy1.Succeeded || !policy2.Succeeded)
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            },
            configureServices: p =>
            {
                p.AddSingleton<IAuthorizationHandler, OpaPolicyHandler<UserPolicyInput>>();
                p.AddAuthorization(pp => pp.AddPolicy("some", policy => policy.RequireAssertion(_ => true)));
                return p;
            }
            );

        var request = new HttpRequestMessage(HttpMethod.Get, $"{server.BaseAddress}");

        var transaction = new Transaction
        {
            Request = request,
            Response = await server.CreateClient().SendAsync(request),
        };

        transaction.ResponseText = await transaction.Response.Content.ReadAsStringAsync();

        Assert.NotNull(transaction.Response);
        Assert.Equal(expected, transaction.Response.StatusCode);
    }

    [Theory]
    [InlineData("u1", HttpStatusCode.OK)]
    [InlineData("wrong", HttpStatusCode.Forbidden)]
    public async Task SimpleNoCompilation(string user, HttpStatusCode expected)
    {
        var compiler = new TestingCompiler();
        await using var policy = await compiler.CompileBundleAsync("./Policy", new());

        var opts = new WasmPolicyEngineOptions
        {
            SerializationOptions = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            },
            StrictBuiltinErrors = true,
        };

        using var server = CreateServerFull(
            output,
            handler: async (context, _) =>
            {
                var azs = context.RequestServices.GetRequiredService<IAuthorizationService>();

                var result = await azs.AuthorizeAsync(context.User, new UserPolicyInput(user), "Opa/az/user");

                if (!result.Succeeded)
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            },
            configureServices: p =>
            {
                p.AddLogging(p => p.AddXunit(output).AddFilter(pp => pp > LogLevel.Trace));
                p.AddSingleton<IAuthorizationHandler, OpaPolicyHandler<UserPolicyInput>>();

                p.AddOpaAuthorization(
                    cfg =>
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        cfg.AddPolicySource(_ => new TestPolicySource(new OpaBundleEvaluatorFactory(policy, opts)));
                        cfg.AddConfiguration(
                            pp =>
                            {
                                pp.AllowedHeaders.Add(".*");
                                pp.EngineOptions = opts;
                            }
                            );
                    }
                    );

                p.AddAuthentication().AddScheme<AuthenticationSchemeOptions, TestAuthenticationSchemeHandler>("Test", null);
                p.AddAuthorization();
            }
            );

        var request = new HttpRequestMessage(HttpMethod.Get, $"{server.BaseAddress}");

        var transaction = new Transaction
        {
            Request = request,
            Response = await server.CreateClient().SendAsync(request),
        };

        transaction.ResponseText = await transaction.Response.Content.ReadAsStringAsync();

        Assert.NotNull(transaction.Response);
        Assert.Equal(expected, transaction.Response.StatusCode);
    }

    [Theory]
    [InlineData("u1", HttpStatusCode.OK)]
    [InlineData("wrong", HttpStatusCode.Forbidden)]
    public async Task ParallelSimple(string user, HttpStatusCode expected)
    {
        using var server = CreateServer(
            output,
            handler: async (context, _) =>
            {
                var azs = context.RequestServices.GetRequiredService<IAuthorizationService>();

                var result = await azs.AuthorizeAsync(context.User, new UserPolicyInput(user), "Opa/az/user");

                if (!result.Succeeded)
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            },
            configureServices: p => p.AddSingleton<IAuthorizationHandler, OpaPolicyHandler<UserPolicyInput>>()
            );

        async Task DoSimple()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{server.BaseAddress}");
            using var client = server.CreateClient();

            var transaction = new Transaction
            {
                Request = request,
                Response = await client.SendAsync(request),
            };

            transaction.ResponseText = await transaction.Response.Content.ReadAsStringAsync();

            Assert.NotNull(transaction.Response);
            Assert.Equal(expected, transaction.Response.StatusCode);
        }

        var options = new ParallelOptions { MaxDegreeOfParallelism = 4 };
        await Parallel.ForEachAsync(
            Enumerable.Range(0, 10_000),
            options,
            async (i, _) => await DoSimple()
            );

        output.WriteLine("Done!");
    }

    [Fact]
    public async Task Jwt()
    {
        using var server = CreateServer(
            output,
            handler: async (context, _) =>
            {
                var request = context.Request;
                var azs = context.RequestServices.GetRequiredService<IAuthorizationService>();

                var result = await azs.AuthorizeAsync(context.User, request, "Opa/az/jwt");

                if (!result.Succeeded)
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }
            );

        var request = new HttpRequestMessage(HttpMethod.Get, server.BaseAddress);

        var handler = new JwtSecurityTokenHandler();

        var sd = new SecurityTokenDescriptor
        {
            Issuer = "opa.tests",
            Claims = new Dictionary<string, object>
            {
                { "user", "jwtUser" },
                { "role", "jwtTester" },
            },
        };

        var token = handler.CreateEncodedJwt(sd);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var transaction = new Transaction
        {
            Request = request,
            Response = await server.CreateClient().SendAsync(request),
        };

        transaction.ResponseText = await transaction.Response.Content.ReadAsStringAsync();

        Assert.NotNull(transaction.Response);
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
    }

    [Theory]
    [InlineData("az/attr")]
    [InlineData("az/svc")]
    [InlineData("az/attr", "xxx", HttpStatusCode.Forbidden)]
    [InlineData("az/svc", "xxx", HttpStatusCode.Forbidden)]
    public async Task RouteAuthorization(string path, string? user = null, HttpStatusCode expected = HttpStatusCode.OK)
    {
        using var server = CreateServer(output);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{server.BaseAddress}{path}");
        var handler = new JwtSecurityTokenHandler();

        var sd = new SecurityTokenDescriptor
        {
            Issuer = "opa.tests",
            Claims = new Dictionary<string, object>
            {
                { "user", user ?? "attrUser" },
                { "role", "attrTester" },
            },
        };

        var token = handler.CreateEncodedJwt(sd);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var transaction = new Transaction
        {
            Request = request,
            Response = await server.CreateClient().SendAsync(request),
        };

        transaction.ResponseText = await transaction.Response.Content.ReadAsStringAsync();

        Assert.NotNull(transaction.Response);
        Assert.Equal(expected, transaction.Response.StatusCode);
    }

    [Fact]
    public async Task Claims()
    {
        using var server = CreateServer(
            output,
            handler: async (context, _) =>
            {
                var request = context.Request;
                var azs = context.RequestServices.GetRequiredService<IAuthorizationService>();

                var claims = new List<Claim>
                {
                    new("claimX", "valueY"),
                };

                var identity = new ClaimsIdentity(claims);
                context.User.AddIdentity(identity);

                var result = await azs.AuthorizeAsync(context.User, request, "Opa/az/claims");

                if (!result.Succeeded)
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }
            );

        var request = new HttpRequestMessage(HttpMethod.Get, server.BaseAddress);
        using var client = server.CreateClient();

        var transaction = new Transaction
        {
            Request = request,
            Response = await client.SendAsync(request),
        };

        transaction.ResponseText = await transaction.Response.Content.ReadAsStringAsync();

        Assert.NotNull(transaction.Response);
        Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
    }

    [Theory]
    [InlineData("u1", HttpStatusCode.OK)]
    [InlineData("u2", HttpStatusCode.Forbidden)]
    public async Task Composite(string user, HttpStatusCode expected)
    {
        using var server = CreateServer(
            output,
            handler: async (context, _) =>
            {
                var azs = context.RequestServices.GetRequiredService<IAuthorizationService>();

                var result = await azs.AuthorizeAsync(context.User, new UserPolicyInput(user), "Opa/complex");

                if (!result.Succeeded)
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            },
            configureServices: p => p.AddSingleton<IAuthorizationHandler, ComplexAuthorizationHandler>()
            );

        var request = new HttpRequestMessage(HttpMethod.Get, $"{server.BaseAddress}");

        var transaction = new Transaction
        {
            Request = request,
            Response = await server.CreateClient().SendAsync(request),
        };

        transaction.ResponseText = await transaction.Response.Content.ReadAsStringAsync();

        Assert.NotNull(transaction.Response);
        Assert.Equal(expected, transaction.Response.StatusCode);
    }

    private record UserPolicyInput([UsedImplicitly] string User);

    [UsedImplicitly]
    private record UserAccessPolicyOutput
    {
        public bool Access { get; [UsedImplicitly] set; }

        public bool Admin { get; [UsedImplicitly] set; }
    }

    private class ComplexAuthorizationHandler(IOpaPolicyService service, IOptions<OpaAuthorizationOptions> options)
        : AuthorizationHandler<OpaPolicyRequirement, UserPolicyInput>
    {
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OpaPolicyRequirement requirement,
            UserPolicyInput resource)
        {
            var result1 = await service.Evaluate<UserPolicyInput, UserAccessPolicyOutput>(resource, requirement.Entrypoint);

            if (result1 is not { Access: true, Admin: true })
                return;

            var inputRaw = JsonSerializer.Serialize(resource, options.Value.EngineOptions?.SerializationOptions);
            var result2Raw = await service.EvaluateRaw(inputRaw.AsMemory(), requirement.Entrypoint);
            var result2 = JsonSerializer.Deserialize<PolicyEvaluationResult<UserAccessPolicyOutput>[]>(
                result2Raw,
                options.Value.EngineOptions?.SerializationOptions
                );

            if (result2![0].Result is { Access: true, Admin: true })
                context.Succeed(requirement);
        }
    }

    private static TestServer CreateServerFull(
        ITestOutputHelper output,
        Func<HttpContext, Func<Task>, Task> handler,
        Action<IServiceCollection> configureServices,
        Uri? baseAddress = null)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(configureServices)
            .Configure(
                app =>
                {
                    app.UseAuthentication();
                    app.UseAuthorization();

                    app.Use(handler);
                }
                );

        var server = new TestServer(builder);

        if (baseAddress != null)
            server.BaseAddress = baseAddress;

        return server;
    }

    private TestServer CreateServer(
        ITestOutputHelper testOutput,
        Func<HttpContext, Func<Task>, Task>? handler = null,
        Uri? baseAddress = null,
        Func<IServiceCollection, IServiceCollection>? configureServices = null)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(
                builder =>
                {
                    builder.AddLogging(p => p.AddXunit(testOutput, LogLevel.Trace));

                    //builder.AddLogging(p => p.AddConsole());

                    builder.AddOpaAuthorization(
                        cfg =>
                        {
                            cfg.AddCompiler<TestingCompiler>();
                            cfg.AddPolicySource<FileSystemPolicySource>();
                            cfg.AddConfiguration(
                                p =>
                                {
                                    p.Compiler = new()
                                    {
                                        OutputPath = _outputDirectory.FullName,
                                        ForceBundleWriter = true,

                                        //Debug = true,
                                    };
                                    p.PolicyBundlePath = "./Policy";
                                    p.AllowedHeaders.Add(".*");
                                    p.IncludeClaimsInHttpRequest = true;
                                    p.EngineOptions = new()
                                    {
                                        SerializationOptions = new()
                                        {
                                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                        },
                                    };
                                }
                                );
                        }
                        );

                    if (handler == null)
                        builder.AddRouting();

                    builder.AddAuthentication()
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationSchemeHandler>("Test", null);
                    builder.AddAuthorization();

                    configureServices?.Invoke(builder);
                }
                )
            .Configure(
                app =>
                {
                    if (handler != null)
                    {
                        app.UseAuthentication();
                        app.UseAuthorization();

                        app.Use(handler);
                    }
                    else
                    {
                        app.UseRouting();

                        app.UseAuthentication();
                        app.UseAuthorization();

                        app.UseEndpoints(
                            p =>
                            {
                                p.MapGet(
                                    "/az/attr",
                                    [OpaPolicyAuthorize("az", "attr")](HttpContext context, CancellationToken ct)
                                        => Task.FromResult(Results.Ok())
                                    );
                                p.MapGet(
                                    "/az/svc",
                                    async ([FromServices] IAuthorizationService azs, ClaimsPrincipal user, HttpContext context) =>
                                    {
                                        var result = await azs.AuthorizeAsync(user, context, "Opa/az/attr");
                                        return result.Succeeded ? Results.Ok() : Results.Forbid();
                                    }
                                    );
                            }
                            );
                    }
                }
                );

        var server = new TestServer(builder);

        if (baseAddress != null)
            server.BaseAddress = baseAddress;

        return server;
    }
}