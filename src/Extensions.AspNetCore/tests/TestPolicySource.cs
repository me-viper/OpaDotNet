using Microsoft.Extensions.Primitives;

using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore.Tests;

internal record TestPolicySource(OpaBundleEvaluatorFactory Factory) : IOpaPolicySource
{
    public IOpaEvaluator CreateEvaluator()
    {
        return Factory.Create();
    }

    private readonly CancellationChangeToken _cct = new(CancellationToken.None);

    public void Dispose() => Factory.Dispose();

    public IChangeToken OnPolicyUpdated() => _cct;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}