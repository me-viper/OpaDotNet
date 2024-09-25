using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

using OpaDotNet.Compilation.Cli;
using OpaDotNet.Extensions.AspNetCore;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Builtins;

var builder = WebApplication.CreateBuilder(args);

// Register core services.
builder.Services.AddOpaAuthorization(
    cfg =>
    {
        // Setup Cli compiler.
        cfg.AddCompiler<RegoCliCompiler>();

        // Get policies from the file system.
        cfg.AddFileSystemPolicySource();

        // Register custom built-ins.
        cfg.AddCustomBuiltins<Custom1, Custom1>();
        cfg.AddCustomBuiltins<Custom2, Custom2>();

        // Configure.
        cfg.AddConfiguration(
            p =>
            {
                p.Compiler = new() { CapabilitiesVersion = "v0.53.0" };

                // Allow to pass all headers as policy query input.
                p.AllowedHeaders.Add(".*");

                p.MonitoringInterval = TimeSpan.FromSeconds(5);

                // Path where look for rego policies.
                p.PolicyBundlePath = "./Policy";
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
app.MapGet("/allow1", [OpaPolicyAuthorize("example", "allow_1")]() => "Custom built-in 1");
app.MapGet("/allow2", [OpaPolicyAuthorize("example", "allow_2")]() => "Custom built-in 2");

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

internal class Custom1 : IOpaCustomBuiltins, ICapabilitiesProvider
{
    public void Reset()
    {}

    [OpaCustomBuiltin("custom1.func")]
    public bool Func(string arg1) => arg1.Equals("/allow1", StringComparison.Ordinal);

    public Stream GetCapabilities()
    {
        var caps = """
            {
                "builtins": [
                  {
                    "name": "custom1.func",
                    "decl": {
                      "type": "function",
                      "args": [
                        {
                          "type": "string"
                        }
                      ],
                      "result": {
                        "type": "boolean"
                      }
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

internal class Custom2 : IOpaCustomBuiltins, ICapabilitiesProvider
{
    public void Reset()
    {}

    [OpaCustomBuiltin("custom2.func")]
    public bool Func(string arg1) => arg1.Equals("/allow2", StringComparison.Ordinal);

    public Stream GetCapabilities()
    {
        // Getting capabilities from resources.
        var result = Assembly.GetExecutingAssembly().GetManifestResourceStream("CustomBuiltins.caps2.json");

        if (result == null)
            throw new InvalidOperationException("Failed to load 'caps2.json' resource");

        return result;
    }
}