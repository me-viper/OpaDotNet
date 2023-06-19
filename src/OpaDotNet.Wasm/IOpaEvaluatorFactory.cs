namespace OpaDotNet.Wasm;

public interface IOpaEvaluatorFactory
{
    IOpaEvaluator CreateWithData<TData>(Stream policy, TData? data, WasmPolicyEngineOptions? options = null);
    
    IOpaEvaluator CreateWithJsonData(Stream policy, string? dataJson = null, WasmPolicyEngineOptions? options = null);
}