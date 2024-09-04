[![CI](https://github.com/me-viper/OpaDotNet.Extensions/actions/workflows/ci.yml/badge.svg)](https://github.com/me-viper/OpaDotNet.Extensions/actions/workflows/ci.yml)
[![Coverage Status](https://coveralls.io/repos/github/me-viper/OpaDotNet.Extensions/badge.svg)](https://coveralls.io/github/me-viper/OpaDotNet.Extensions)

# Open Policy Agent (OPA) Extensions

This repository contains AspNetCore specific extensions for [OpaDotNet](https://github.com/me-viper/OpaDotNet) project.

## NuGet Packages

|                                 | Official                                                                                                                                        | Preview                                                                                                                                            |
|---------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------|
| OpaDotNet.Extensions.AspNetCore | [![NuGet](https://img.shields.io/nuget/v/OpaDotNet.Extensions.AspNetCore.svg)](https://www.nuget.org/packages/OpaDotNet.Extensions.AspNetCore/) | [![Nuget](https://img.shields.io/nuget/vpre/OpaDotNet.Extensions.AspNetCore.svg)](https://www.nuget.org/packages/OpaDotNet.Extensions.AspNetCore/) |

## Getting Started

### Install nuget package

```sh
dotnet add package OpaDotNet.Extensions.AspNetCore
```

### Usage

Add policy file `./Policy/policy.rego`

```rego
package example

import future.keywords.if

# METADATA
# entrypoint: true
allow if {
    true
}

# METADATA
# entrypoint: true
deny if {
    false
}
```

The code:

```csharp
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

using OpaDotNet.Extensions.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register core services.
builder.Services.AddOpaAuthorization(
    cfg =>
    {
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
app.MapGet("/allow", [OpaPolicyAuthorize("example", "allow")] () => "Hi!");

// Authorize attribute works too. Policy needs to be named 'Opa/{module}/{entrypoint}'.
app.MapGet("/allow2", [Authorize("Opa/example/allow")]() => "Hi!");

// Will evaluate example/deny rule and return 403.
app.MapGet("/deny", [OpaPolicyAuthorize("example", "deny")] () => "Should not be here!");

app.Run();


internal class NopAuthenticationSchemeHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationSchemeName = "Nop";

    public NopAuthenticationSchemeHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
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
```

## Samples

* [AspNetCore](./samples/WebApp/)
* [Yarp](./samples/YarpApp/)

## 3rd Party Libraries and Contributions

* [xUnit.net](https://xunit.net/) - Free, open source, community-focused unit testing tool for the .NET Framework.
