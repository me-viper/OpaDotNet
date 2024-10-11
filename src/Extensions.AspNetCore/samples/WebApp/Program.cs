using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using OpaDotNet.Compilation.Cli;
using OpaDotNet.Extensions.AspNetCore;
using OpaDotNet.Wasm;

var builder = WebApplication.CreateBuilder(args);

// Register core services.
builder.Services.AddOpaAuthorization(
    cfg =>
    {
        // Setup Cli compiler.
        cfg.AddCompiler<RegoCliCompiler>();

        // Get policies from the file system.
        cfg.AddFileSystemPolicySource();

        // Configure.
        cfg.AddConfiguration(
            p =>
            {
                // Allow to pass all headers as policy query input.
                p.AllowedHeaders.Add(".*");

                // Path where look for rego policies.
                p.PolicyBundlePath = "./Policy";

                p.EngineOptions = WasmPolicyEngineOptions.DefaultWithJsonOptions(
                    pp => pp.PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    );
            }
            );
    }
    );

// Handler with custom policy input.
builder.Services.AddSingleton<IAuthorizationHandler, OpaPolicyHandler<ResourcePolicyInput>>();

// In real scenarios here will be more sophisticated authentication.
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, NopAuthenticationSchemeHandler>(
        NopAuthenticationSchemeHandler.AuthenticationSchemeName,
        null
        );

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Will evaluate example/allow rule and return 200.
app.MapGet("/allow", [OpaPolicyAuthorize("example", "allow")]() => "Hi!");

// Authorize attribute works too. Policy needs to be named 'Opa/{module}/{entrypoint}'.
app.MapGet("/allow2", [Authorize("Opa/example/allow")]() => "Hi!");

// Will evaluate example/deny rule with IAuthorizationService and return 403.
app.MapGet(
    "/deny",
    ([FromServices] IAuthorizationService azs, HttpContext context, ClaimsPrincipal user) =>
    {
        var result = azs.AuthorizeAsync(user, context, "opa/example/deny");
        return result.Result.Succeeded ? Results.Ok("Should not be here!") : Results.Forbid();
    }
    );

// Will evaluate example/check_resource policy end return 200 if resource == allowed; 403 otherwise.
app.MapGet(
    "/resource/{name}", ([FromServices] IAuthorizationService azs, ClaimsPrincipal user, string name) =>
    {
        var result = azs.AuthorizeAsync(user, new ResourcePolicyInput(name), "opa/example/check_resource");
        return result.Result.Succeeded ? Results.Ok($"Got access to {name}") : Results.Forbid();
    }
    );

app.Run();

internal record ResourcePolicyInput(string Resource);

internal class NopAuthenticationSchemeHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationSchemeName = "Nop";

    public NopAuthenticationSchemeHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var principal = new ClaimsPrincipal();
        var ticket = new AuthenticationTicket(principal, AuthenticationSchemeName);
        var result = AuthenticateResult.Success(ticket);

        return Task.FromResult(result);
    }
}