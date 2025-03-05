namespace OpaDotNet.Wasm;

/// <summary>
/// A factory abstraction for a component that can create <see cref="IOpaEvaluator"/> instances.
/// </summary>
public interface IOpaEvaluatorFactory : IDisposable
{
    /// <summary>
    /// Creates new OPA evaluator instance
    /// </summary>
    /// <returns>New OPA evaluator instance</returns>
    IOpaEvaluator Create();
}