using Microsoft.AspNetCore.Authorization;

namespace Opa.WebApp.Policy;

public class OpaPolicyHandler : AuthorizationHandler<OpaPolicyRequirement>
{
    private readonly OpaPolicyEvaluatorProvider _evaluatorProvider;
    
    private readonly ILogger _logger;
    
    public OpaPolicyHandler(OpaPolicyEvaluatorProvider evaluatorProvider, ILogger<OpaPolicyHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(evaluatorProvider);
        
        _evaluatorProvider = evaluatorProvider;
        _logger = logger;
    }
    
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        OpaPolicyRequirement requirement)
    {
        var user = context.User.Identity?.Name;
        
        if (string.IsNullOrWhiteSpace(user))
            return;
        
        var opaPolicy = await _evaluatorProvider.GetPolicyEvaluator();
        var input = new OpaPolicyInput(user, context.Resource as string);
        
        using var scope = _logger.BeginScope(new { requirement.PolicyName, input.User, input.Resource });
        _logger.LogDebug("Evaluating policy");
        
        try
        {
            var result = opaPolicy.EvaluatePredicate(
                input, 
                requirement.PolicyName
                );
            
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