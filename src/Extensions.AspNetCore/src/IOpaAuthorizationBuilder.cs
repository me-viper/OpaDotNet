using Microsoft.Extensions.DependencyInjection;

namespace OpaDotNet.Extensions.AspNetCore;

public interface IOpaAuthorizationBuilder
{
    IServiceCollection Services { get; }
}