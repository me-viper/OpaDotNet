using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpaDotNet.Extensions.AspNetCore.Tests;

internal class AlwaysFailAuthenticationSchemeHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
#if NET8_0_OR_GREATER
    public AlwaysFailAuthenticationSchemeHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }
#else
    public AlwaysFailAuthenticationSchemeHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }
#endif

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var result = AuthenticateResult.NoResult();
        return Task.FromResult(result);
    }
}