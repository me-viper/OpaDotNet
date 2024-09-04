using Microsoft.Extensions.DependencyInjection;

namespace OpaDotNet.Extensions.AspNetCore;

internal sealed class OpaAuthorizationBuilder : IOpaAuthorizationBuilder
{
    private readonly HashSet<string> _authenticationSchemes = new();

    public IServiceCollection Services { get; }

    public IReadOnlySet<string> AuthenticationSchemes => _authenticationSchemes;

    public OpaAuthorizationBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }
}