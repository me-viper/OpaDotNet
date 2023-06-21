using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

using Opa.WebApp.Infra;

namespace Opa.WebApp.Policy;

public class OpaPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _default;

    public OpaPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _default = new DefaultAuthorizationPolicyProvider(options);
    }
    
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = new AuthorizationPolicyBuilder(BasicAuthenticationHandler.BasicAuth);
        
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new OpaPolicyRequirement(policyName));

        return Task.FromResult<AuthorizationPolicy?>(policy.Build());
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _default.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return Task.FromResult<AuthorizationPolicy?>(null);
    }
}