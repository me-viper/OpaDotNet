using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

namespace OpaDotNet.Tests.Common;

internal static class EvaluatorExtensions
{
    public static TResult EvaluateValue<TResult>(
        this IOpaEvaluator eval,
        TResult value,
        string entrypoint,
        string? input = null)
        where TResult : notnull
    {
        var result = eval.Evaluate<object?, TResult>(input, entrypoint);
        return result.Result;
    }
}