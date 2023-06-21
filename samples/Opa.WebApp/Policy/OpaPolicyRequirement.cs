using Microsoft.AspNetCore.Authorization;

namespace Opa.WebApp.Policy;

public class OpaPolicyRequirement : IAuthorizationRequirement
{
    public OpaPolicyRequirement(string policyName)
    {
        PolicyName = policyName;
    }

    public string PolicyName { get; set; }
}