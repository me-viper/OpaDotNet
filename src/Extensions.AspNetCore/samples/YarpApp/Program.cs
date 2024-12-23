using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

using OpaDotNet.Compilation.Interop;
using OpaDotNet.Extensions.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddYamlFile("appsettings.yaml", false, true);

// Custom policy handler.
// Should be registered before AddOpaAuthorization is called.
builder.Services.AddSingleton<IAuthorizationHandler, CustomPolicyHandler>();

builder.Services.AddOpaAuthorization(
    cfg =>
    {
        // Setup Interop compiler.
        cfg.AddCompiler<RegoInteropCompiler>();

        // Get policies from the configuration.
        cfg.AddConfigurationPolicySource(builder.Configuration.GetSection("policies"));
        cfg.AddConfiguration(builder.Configuration.GetSection("Opa"));
        cfg.AddJsonOptions(p => p.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
    }
    );

// In real scenarios here will be more sophisticated authentication.
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, NopAuthenticationSchemeHandler>(
        NopAuthenticationSchemeHandler.AuthenticationSchemeName,
        null
        );

builder.Services.AddAuthorization();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Ensure build path exists.
var buildPath = app.Services.GetService<IOptions<RegoCompilerOptions>>()?.Value.OutputPath;

if (!string.IsNullOrWhiteSpace(buildPath))
{
    if (!Directory.Exists(buildPath))
        Directory.CreateDirectory(buildPath);
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();

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

internal record PolicyResult
{
    public DenyReason[] Deny { get; set; } = [];
}

internal record DenyReason
{
    public string? Reason { get; set; }
};

internal class CustomPolicyHandler : OpaPolicyHandler
{
    public CustomPolicyHandler(
        IOpaPolicyService service,
        IOptions<OpaAuthorizationOptions> options,
        ILogger<CustomPolicyHandler> logger) : base(service, options, logger)
    {
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OpaPolicyRequirement requirement,
        IHttpRequestPolicyInput resource)
    {
        Logger.LogDebug("Evaluating policy");

        // We expect policies to return something more descriptive rather than yes/no.
        var result = await Service.Evaluate<IHttpRequestPolicyInput, PolicyResult>(resource, requirement.Entrypoint);

        if (result.Deny.Length == 0)
        {
            Logger.LogDebug("Authorization policy succeeded");
            context.Succeed(requirement);
        }
        else
        {
            var reasons = string.Join("\n", result.Deny.Select(p => p.Reason));
            Logger.LogDebug("Authorization policy denied:\n {Reasons}", reasons);
        }
    }
}