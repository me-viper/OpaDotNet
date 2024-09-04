using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;

using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore;

/// <summary>
/// Opa policy source.
/// </summary>
public interface IOpaPolicySource : IDisposable, IHostedService
{
    /// <summary>
    /// Creates policy evaluator instance.
    /// </summary>
    IOpaEvaluator CreateEvaluator();

    /// <summary>
    /// Propagates notifications that a policy recompilation has occurred.
    /// </summary>
    /// <returns></returns>
    IChangeToken OnPolicyUpdated();
}