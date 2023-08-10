using OpaDotNet.Wasm;

namespace OpaDotNet.Tests.Common;

internal static class TestHelpers
{
    public const string SimplePolicySource = """
        package example
        import future.keywords.if
        default allow := false
        """;

    public static readonly string[] SimplePolicyEntrypoints = { "example/allow" };

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