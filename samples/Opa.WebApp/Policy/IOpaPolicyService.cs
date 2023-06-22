using OpaDotNet.Wasm;

namespace Opa.WebApp.Policy;

public interface IOpaPolicyService : IDisposable
{
    IOpaEvaluator Evaluator { get; }
}