using Microsoft.AspNetCore.Authentication;

namespace Opa.WebApp.Infra;

internal class BasicAuthenticationFailedContext : ResultContext<AuthenticationSchemeOptions>
{
    public BasicAuthenticationFailedContext(
        HttpContext context,
        AuthenticationScheme scheme,
        AuthenticationSchemeOptions options)
        : base(context, scheme, options)
    {
    }

    public Exception? Exception { get; set; }
}