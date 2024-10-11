using OpaDotNet.Wasm;

namespace OpaDotNet.Extensions.AspNetCore;

public interface IOpaBundleEvaluatorFactoryBuilder
{
    OpaEvaluatorFactory Build(Stream policy);
}