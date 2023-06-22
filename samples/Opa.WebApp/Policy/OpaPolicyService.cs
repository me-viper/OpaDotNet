using Microsoft.Extensions.Options;

using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

namespace Opa.WebApp.Policy;

public sealed class OpaPolicyService : IHostedService, IOpaPolicyService
{
    private readonly IRegoCompiler _compiler;
    
    private readonly IOptions<OpaPolicyEvaluatorProviderOptions> _options;
    
    private IOpaEvaluator? _evaluator;
    
    private readonly ILogger _logger;
    
    public IOpaEvaluator Evaluator
    {
        get
        {
            if (_evaluator == null)
                throw new InvalidOperationException("Failed to initialize evaluator");
            
            return _evaluator;
        }
    }

    public OpaPolicyService(
        IRegoCompiler compiler, 
        IOptions<OpaPolicyEvaluatorProviderOptions> options,
        ILogger<OpaPolicyService> logger)
    {
        ArgumentNullException.ThrowIfNull(compiler);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        
        _compiler = compiler;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Performing initial bundle compilation");
        
        await using var policy = await _compiler.CompileBundle(
            _options.Value.PolicyBundlePath, 
            cancellationToken: cancellationToken
            );
                    
        var factory = new OpaEvaluatorFactory();
        _evaluator = factory.CreateFromBundle(policy);
        
        _logger.LogDebug("Compilation succeeded");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _evaluator?.Dispose();
    }
}