﻿using OpaDotNet.Extensions.AspNetCore.Telemetry;

namespace OpaDotNet.Extensions.AspNetCore;

[PublicAPI]
public class OpaPolicyHandler<TResource> : AuthorizationHandler<OpaPolicyRequirement, TResource>
{
    protected IOpaPolicyService Service { get; }

    protected ILogger Logger { get; }

    public OpaPolicyHandler(IOpaPolicyService service, ILogger<OpaPolicyHandler<TResource>> logger)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(logger);

        Service = service;
        Logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OpaPolicyRequirement requirement,
        TResource resource)
    {
        using var scope = Logger.BeginScope("Entrypoint {Entrypoint}", requirement.Entrypoint);

        try
        {
            Logger.PolicyEvaluating();
            var result = await Service.EvaluatePredicate(resource, requirement.Entrypoint).ConfigureAwait(false);

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
        }
        catch (Exception ex)
        {
            Logger.PolicyFailed(ex);
            OpaEventSource.Log.PolicyFailed(requirement.Entrypoint);
        }
    }
}