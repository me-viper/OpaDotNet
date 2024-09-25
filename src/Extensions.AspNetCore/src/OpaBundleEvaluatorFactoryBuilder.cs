using Microsoft.Extensions.Options;

using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore;

internal class OpaBundleEvaluatorFactoryBuilder(IOptionsMonitor<OpaAuthorizationOptions> options, IBuiltinsFactory builtins)
    : IOpaBundleEvaluatorFactoryBuilder
{
    public OpaEvaluatorFactory Build(Stream policy) => new OpaBundleEvaluatorFactory(policy, options.CurrentValue.EngineOptions, builtins);
}