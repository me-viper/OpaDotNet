using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Opa.WebApp.Infra;
using Opa.WebApp.Policy;

using OpaDotNet.Wasm.Compilation;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(p => p.IncludeScopes = true);

builder.Services.Configure<OpaPolicyEvaluatorProviderOptions>(p => p.PolicyBundlePath = "./OpaBundle");

builder.Services.AddSingleton<IAuthorizationPolicyProvider, OpaPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, OpaPolicyHandler>();

builder.Services.AddSingleton<IRegoCompiler, RegoCliCompiler>();
builder.Services.AddSingleton<OpaPolicyService>();
builder.Services.AddHostedService(p => p.GetRequiredService<OpaPolicyService>());
builder.Services.AddSingleton<IOpaPolicyService>(p => p.GetRequiredService<OpaPolicyService>());

builder.Services
    .AddAuthentication(BasicAuthenticationHandler.BasicAuth)
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
        BasicAuthenticationHandler.BasicAuth,
        options =>
        {
            options.Events = new BasicAuthenticationEvents
            {
                OnValidateCredentials = context =>
                {
                    if (string.IsNullOrWhiteSpace(context.Username))
                        return Task.CompletedTask;
                    
                    // For sample purposes we don't bother with checking passwords.
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, 
                            context.Username, 
                            ClaimValueTypes.String, 
                            context.Options.ClaimsIssuer),
                        new Claim(
                            ClaimTypes.Name, 
                            context.Username, 
                            ClaimValueTypes.String, 
                            context.Options.ClaimsIssuer)
                    };
                    
                    context.Principal = new (new ClaimsIdentity(claims, context.Scheme.Name));
                    context.Success();
                    
                    return Task.CompletedTask;
                }
            };
        });

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

// Evaluate example/user policy for user.
app.MapGet("/", [Authorize("example/user")] (ClaimsPrincipal user) => $"Hello {user.Identity?.Name}!");
app.MapGet(
    "/resource/{id}", 
    async ([FromServices] IAuthorizationService authorizationService, ClaimsPrincipal user, string id) =>
    {
        // Evaluate example/resource policy for user and resource.
        var result = await authorizationService.AuthorizeAsync(user, id, "example/resource");
        
        if (result.Succeeded)
            return Results.Ok($"You can access resource {id}");
        
        return Results.Forbid();
    });

app.Run();