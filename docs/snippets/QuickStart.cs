// ReSharper disable RedundantUsingDirective

#pragma warning disable CS0105

#region Usings

using OpaDotNet.Wasm;

#endregion

#region CompilationUsings

using OpaDotNet.Wasm;
using OpaDotNet.Compilation.Cli;

#endregion

#pragma warning restore CS0105

namespace OpaDotNet.Tests.Snippets;

[Trait("Snippet", "true")]
public partial class Snippets
{
    [Fact]
    public void QuickStartEval()
    {
        #region QuickStartLoad

        const string data = "{ \"world\": \"world\" }";

        using var engine = OpaEvaluatorFactory.CreateFromWasm(
            File.OpenRead("data/policy.wasm")
            );

        engine.SetDataFromRawJson(data);

        #endregion

        #region QuickStartEval

        var input = new { message = "world" };
        var policyResult = engine.EvaluatePredicate(input);

        #endregion

        #region QuickStartCheck

        if (policyResult.Result)
        {
            // We've been authorized.
        }
        else
        {
            // Can't do that.
        }

        #endregion

        Assert.True(policyResult.Result);
    }

    [Fact]
    public async Task QuickStartCompileCli()
    {
        #region QuickStartCompilation

        var compiler = new RegoCliCompiler();
        var policyStream = await compiler.CompileFile("quickstart/example.rego", new[] { "example/hello" });

        // Use compiled policy.
        using var engine = OpaEvaluatorFactory.CreateFromBundle(policyStream);

        #endregion

        const string data = "{ \"world\": \"world\" }";
        engine.SetDataFromRawJson(data);

        var result = engine.EvaluatePredicate(new { message = "world" });

        Assert.True(result.Result);
    }
}