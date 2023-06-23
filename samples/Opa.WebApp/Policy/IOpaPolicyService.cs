using OpaDotNet.Wasm;

namespace Opa.WebApp.Policy;

public interface IOpaPolicyService : IDisposable
{
    Task<PolicyEvaluationResult<bool>> EvaluatePredicate(
        OpaPolicyInput input, 
        string policyName, 
        CancellationToken cancellationToken = default);
}