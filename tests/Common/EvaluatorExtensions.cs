using System.Text.Json.Nodes;

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

    public static async Task<Stream> Compile(this IRegoCompiler compiler, string source, string entrypoint)
    {
        var fileName = $"{Guid.NewGuid()}.rego";

        try
        {
            await File.WriteAllTextAsync(fileName, source);
            return await compiler.CompileFile(fileName, new[] { entrypoint });
        }
        finally
        {
            File.Delete(fileName);
        }
    }
}