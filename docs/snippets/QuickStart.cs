// ReSharper disable RedundantUsingDirective

#region Usings

using OpaDotNet.Wasm;

#endregion

#region CompilationUsings

using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

#endregion

namespace OpaDotNet.Tests.Snippets;

public partial class Snippets
{
    [Fact(Skip = "Documentation sample")]
    public void QuickStartEval()
    {
        #region QuickStartLoad

        const string data = "{ \"world\": \"world\" }";

        using var engine = OpaEvaluatorFactory.CreateFromWasm(
            File.OpenRead("policy.wasm")
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
    }

    [Fact(Skip = "Documentation sample")]
    public async Task QuickStartCompile()
    {
        #region QuickStartCompilation

        var compiler = new RegoCliCompiler();
        var policyStream = await compiler.CompileFile("example.rego", new[] { "example/hello" });

        // Use compiled policy.
        using var engine = OpaEvaluatorFactory.CreateFromBundle(policyStream);

        #endregion
    }
}