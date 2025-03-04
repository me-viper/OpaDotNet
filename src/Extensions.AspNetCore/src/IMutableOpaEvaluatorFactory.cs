using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore;

public interface IMutableOpaEvaluatorFactory : IOpaEvaluatorFactory
{
    void UpdatePolicy(Stream source, WasmPolicyEngineOptions options);
}