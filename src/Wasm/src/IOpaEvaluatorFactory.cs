namespace OpaDotNet.Wasm;

[PublicAPI]
public interface IOpaEvaluatorFactory
{
    IOpaEvaluator CreateFromWasm(Stream policyWasm);

    IOpaEvaluator CreateFromBundle(Stream bundle);
}