using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpaDotNet.Extensions.AspNetCore.Tests;

internal class TestAuthenticationSchemeHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
#if NET8_0_OR_GREATER
    public TestAuthenticationSchemeHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }
#else
    public TestAuthenticationSchemeHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }
#endif

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "Test"), new Claim("scheme", Scheme.Name)],
            "Test"
            );

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        var result = AuthenticateResult.Success(ticket);

        return Task.FromResult(result);
    }
}