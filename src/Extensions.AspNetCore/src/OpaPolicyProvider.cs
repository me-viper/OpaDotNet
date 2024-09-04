using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using Microsoft.Extensions.Options;

namespace OpaDotNet.Extensions.AspNetCore;

[PublicAPI]
public class OpaPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly IAuthorizationPolicyProvider _default;

    private readonly IReadOnlySet<string> _authenticationSchemes;

    public OpaPolicyProvider(IOptions<AuthorizationOptions> options, IOptions<OpaAuthorizationOptions> opaOptions)
        : this(options, opaOptions, new DefaultAuthorizationPolicyProvider(options))
    {
    }

    public OpaPolicyProvider(
        IOptions<AuthorizationOptions> options,
        IOptions<OpaAuthorizationOptions> opaOptions,
        IAuthorizationPolicyProvider defaultProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(defaultProvider);

        _authenticationSchemes = opaOptions.Value.AuthenticationSchemes;
        _default = defaultProvider;
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!OpaPolicyRequirement.TryParse(policyName, out var opr))
            return _default.GetPolicyAsync(policyName);

        var policy = new AuthorizationPolicyBuilder(_authenticationSchemes.ToArray());

        policy.AddRequirements(opr);
        return Task.FromResult<AuthorizationPolicy?>(policy.Build());
    }

    [ExcludeFromCodeCoverage]
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _default.GetDefaultPolicyAsync();
    }

    [ExcludeFromCodeCoverage]
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _default.GetFallbackPolicyAsync();
    }
}