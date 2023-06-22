namespace OpaDotNet.Wasm;

public interface IOpaEvaluatorFactory
{
    IOpaEvaluator CreateFromBundle(Stream policyBundle, WasmPolicyEngineOptions? options = null);
    
    IOpaEvaluator CreateFromWasm(Stream policyWasm, WasmPolicyEngineOptions? options = null);
}