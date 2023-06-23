using JetBrains.Annotations;

namespace OpaDotNet.Wasm;

[PublicAPI]
public interface IOpaEvaluatorFactory
{
    /// <summary>
    /// Creates evaluator from compiled policy bundle.
    /// </summary>
    /// <remarks>
    /// Loads policy (policy.wasm) and external data (data.json) from the bundle. 
    /// </remarks>
    /// <param name="policyBundle">Compiled policy bundle (*.tar.gz)</param>
    /// <param name="options">Evaluator configuration</param>
    /// <returns>Evaluator instance</returns>
    IOpaEvaluator CreateFromBundle(Stream policyBundle, WasmPolicyEngineOptions? options = null);
    
    /// <summary>
    /// Creates evaluator from compiled wasm policy file.
    /// </summary>
    /// <remarks>
    /// If evaluator requires external data it should be loaded manually.
    /// </remarks>
    /// <param name="policyWasm">Compiled wasm policy file (*.wasm)</param>
    /// <param name="options">Evaluator configuration</param>
    /// <returns>Evaluator instance</returns>
    IOpaEvaluator CreateFromWasm(Stream policyWasm, WasmPolicyEngineOptions? options = null);
}