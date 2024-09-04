using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

using OpaDotNet.Compilation.Abstractions;
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

        // Register custom built-ins.
        cfg.AddImportsAbi<CompositeCustomAbi>();

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

internal class Custom1 : CoreImportsAbi, ICapabilitiesProvider
{
    public override object? Func(BuiltinContext context, BuiltinArg arg1)
    {
        if (context.FunctionName.Equals("custom1.func", StringComparison.OrdinalIgnoreCase))
            return arg1.As<string>().Equals("/allow1", StringComparison.Ordinal);

        return base.Func(context, arg1);
    }

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

internal class Custom2 : CoreImportsAbi, ICapabilitiesProvider
{
    public override object? Func(BuiltinContext context, BuiltinArg arg1)
    {
        if (context.FunctionName.Equals("custom2.func", StringComparison.OrdinalIgnoreCase))
            return arg1.As<string>().Equals("/allow2", StringComparison.Ordinal);

        return base.Func(context, arg1);
    }

    public Stream GetCapabilities()
    {
        // Getting capabilities from resources.
        var result = Assembly.GetExecutingAssembly().GetManifestResourceStream("CustomBuiltins.caps2.json");

        if (result == null)
            throw new InvalidOperationException("Failed to load 'caps2.json' resource");

        return result;
    }
}

internal class CompositeCustomAbi : CoreImportsAbi, ICapabilitiesProvider
{
    private readonly Custom1 _custom1 = new();
    private readonly Custom2 _custom2 = new();

    public override object? Func(BuiltinContext context, BuiltinArg arg1)
    {
        if (context.FunctionName.StartsWith("custom1"))
            return _custom1.Func(context, arg1);

        if (context.FunctionName.StartsWith("custom2"))
            return _custom2.Func(context, arg1);

        return base.Func(context, arg1);
    }

    public Stream GetCapabilities()
    {
        // Merge Custom1 and Custom2 capabilities.
        return BundleWriter.MergeCapabilities(_custom1.GetCapabilities(), _custom2.GetCapabilities());
    }
}