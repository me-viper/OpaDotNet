using Microsoft.Extensions.Options;

using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

namespace Opa.WebApp.Policy;

public sealed class OpaPolicyEvaluatorProvider : IDisposable
{
    private readonly RegoCliCompiler _compiler;
    
    private readonly IOptions<OpaPolicyEvaluatorProviderOptions> _options;
    
    private IOpaEvaluator? _evaluator;

    public OpaPolicyEvaluatorProvider(RegoCliCompiler compiler, IOptions<OpaPolicyEvaluatorProviderOptions> options)
    {
        _compiler = compiler;
        _options = options;
    }

    public async ValueTask<IOpaEvaluator> GetPolicyEvaluator()
    {
        if (_evaluator == null)
        {
            await using var policy = await _compiler.CompileBundle(_options.Value.PolicyBundlePath);
            var factory = new OpaEvaluatorFactory();
            _evaluator = factory.CreateFromBundle(policy);
        }
        
        return _evaluator;
    }

    public void Dispose()
    {
        _evaluator?.Dispose();
    }
}