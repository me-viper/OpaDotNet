using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore;

internal sealed class MutableOpaEvaluatorFactory : IMutableOpaEvaluatorFactory
{
    private OpaBundleEvaluatorFactory? _inner;

    private OpaBundleEvaluatorFactory Inner
    {
        get => _inner ?? throw new InvalidOperationException("Evaluator factory have not been initialized");
    }

    public void Dispose()
    {
        _inner?.Dispose();
    }

    public void UpdatePolicy(Stream source, WasmPolicyEngineOptions options)
    {
        var old = _inner;
        _inner = new OpaBundleEvaluatorFactory(source, options);
        old?.Dispose();
    }

    public IOpaEvaluator Create() => Inner.Create();
}