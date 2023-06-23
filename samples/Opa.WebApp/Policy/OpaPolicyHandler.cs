using Microsoft.AspNetCore.Authorization;

using OpaDotNet.Wasm;

namespace Opa.WebApp.Policy;

public class OpaPolicyHandler : AuthorizationHandler<OpaPolicyRequirement>
{
    private readonly IOpaPolicyService _service;
    
    private readonly ILogger _logger;
    
    public OpaPolicyHandler(IOpaPolicyService service, ILogger<OpaPolicyHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(service);
        
        _service = service;
        _logger = logger;
    }
    
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        OpaPolicyRequirement requirement)
    {
        var user = context.User.Identity?.Name;
        
        if (string.IsNullOrWhiteSpace(user))
            return;
        
        var input = new OpaPolicyInput(user, context.Resource as string);
        
        using var scope = _logger.BeginScope(new { requirement.PolicyName, input.User, input.Resource });
        
        try
        {
            _logger.LogDebug("Evaluating policy");
            var result = await _service.EvaluatePredicate(input, requirement.PolicyName);
            
            if (result.Result)
            {
                _logger.LogDebug("Success");
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogDebug("Failed");
                context.Fail(new(this, $"Authorization request denied by policy {requirement.PolicyName}"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authorization policy failed");
            context.Fail(new(this, "Authorization policy failed"));
        }
    }
}