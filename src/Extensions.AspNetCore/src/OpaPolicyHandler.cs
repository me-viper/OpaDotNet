using JetBrains.Annotations;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using OpaDotNet.Extensions.AspNetCore.Telemetry;

namespace OpaDotNet.Extensions.AspNetCore;

[PublicAPI]
public class OpaPolicyHandler : AuthorizationHandler<OpaPolicyRequirement>
{
    protected IOpaPolicyService Service { get; }

    protected ILogger Logger { get; }

    protected IOptions<OpaAuthorizationOptions> Options { get; }

    public OpaPolicyHandler(
        IOpaPolicyService service,
        IOptions<OpaAuthorizationOptions> options,
        ILogger<OpaPolicyHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        Service = service;
        Options = options;
        Logger = logger;
    }

    protected virtual Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OpaPolicyRequirement requirement,
        IHttpRequestPolicyInput resource)
    {
        Logger.PolicyEvaluating();
        var result = Service.EvaluatePredicate(resource, requirement.Entrypoint);

        if (!result)
        {
            Logger.PolicyDenied();
            OpaEventSource.Log.PolicyDenied(requirement.Entrypoint);
        }
        else
        {
            Logger.PolicyAllowed();
            OpaEventSource.Log.PolicyAllowed(requirement.Entrypoint);

            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OpaPolicyRequirement requirement)
    {
        using var scope = Logger.BeginScope(new Dictionary<string, object> { { "Entrypoint", requirement.Entrypoint } });

        try
        {
            HttpRequest request;

            if (context.Resource is HttpContext httpContext)
                request = httpContext.Request;
            else
            {
                if (context.Resource is not HttpRequest rq)
                    return Task.CompletedTask;

                request = rq;
            }

            var input = IHttpRequestPolicyInput.Build(
                request,
                Options.Value.AllowedHeaders,
                Options.Value.IncludeClaimsInHttpRequest ? context.User.Claims : null
                );

            return HandleRequirementAsync(context, requirement, input);
        }
        catch (Exception ex)
        {
            Logger.PolicyFailed(ex);
            OpaEventSource.Log.PolicyFailed(requirement.Entrypoint);
        }

        return Task.CompletedTask;
    }
}