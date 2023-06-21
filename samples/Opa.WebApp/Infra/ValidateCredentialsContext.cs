using Microsoft.AspNetCore.Authentication;

namespace Opa.WebApp.Infra;

internal class ValidateCredentialsContext : ResultContext<AuthenticationSchemeOptions>
{
    public ValidateCredentialsContext(
        HttpContext context,
        AuthenticationScheme scheme,
        AuthenticationSchemeOptions options)
        : base(context, scheme, options)
    {
    }

    public string? Username { get; set; }

    public string? Password { get; set; }
}