namespace OpaDotNet.Wasm;

public interface IOpaEvaluatorFactory : IDisposable
{
    IOpaEvaluator Create();
}