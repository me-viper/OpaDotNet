using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Opa.WebApp.Infra;
using Opa.WebApp.Policy;

using OpaDotNet.Wasm.Compilation;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(p => p.IncludeScopes = true);

builder.Services.Configure<OpaPolicyBuilderOptions>(p => p.PolicyBundlePath = "./OpaBundle");

builder.Services.AddSingleton<RegoCliCompiler>();
builder.Services.AddSingleton<OpaPolicyEvaluatorProvider>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, OpaPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, OpaPolicyHandler>();

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

app.MapGet("/", [Authorize("example/user")] (ClaimsPrincipal user) => $"Hello {user.Identity?.Name}!");
app.MapGet(
    "/resource/{id}", 
    async ([FromServices] IAuthorizationService authorizationService, ClaimsPrincipal user, string id) =>
    {
        var result = await authorizationService.AuthorizeAsync(user, id, "example/resource");
        
        if (result.Succeeded)
            return Results.Ok($"You can access resource {id}");
        
        return Results.Forbid();
    });

app.Run();