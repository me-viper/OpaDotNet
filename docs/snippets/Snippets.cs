// ReSharper disable RedundantUsingDirective

#region Usings

using OpaDotNet.Wasm;

#endregion

#region CompilationUsings

using OpaDotNet.Wasm;
using OpaDotNet.Compilation.Cli;

#endregion

namespace OpaDotNet.Tests.Snippets;

public partial class Snippets
{
    [Fact(Skip = "Documentation sample")]
    public void EvalWasm()
    {
        #region EvalWasm

        // Create evaluator from compiled policy module.
        using var engine = OpaEvaluatorFactory.CreateFromWasm(File.OpenRead("policy.wasm"));

        // Set external data.
        var data = "{\"password\":\"pwd\"}";
        engine.SetDataFromRawJson(data);

        // Evaluate. Policy query will return false.
        var deny = engine.EvaluatePredicate(new { password = "wrong!" });

        if (deny.Result)
        {
            // Should not get here.
        }
        else
        {
            // Wrong password.
        }

        // Evaluate. Policy query will return true.
        var approve = engine.EvaluatePredicate(new { password = "pwd" });

        if (approve.Result)
        {
            // Correct password.
        }
        else
        {
            // Should not get here.
        }

        #endregion
    }

    [Fact(Skip = "Documentation sample")]
    public void EvalSource()
    {
        #region EvalBundle

        // Create evaluator from compiled policy module.
        using var engine = OpaEvaluatorFactory.CreateFromBundle(File.OpenRead("bundle.tar.gz"));

        // External data is in the bundle already.

        // Evaluate. Policy query will return false.
        var deny = engine.EvaluatePredicate(new { password = "wrong!" });

        if (deny.Result)
        {
            // Should not get here.
        }
        else
        {
            // Wrong password.
        }

        // Evaluate. Policy query will return true.
        var approve = engine.EvaluatePredicate(new { password = "pwd" });

        if (approve.Result)
        {
            // Correct password.
        }
        else
        {
            // Should not get here.
        }

        #endregion
    }

    [Fact(Skip = "Documentation sample")]
    public async Task CompileFile()
    {
        #region CompileFile

        var compiler = new RegoCliCompiler();

        var policy = await compiler.CompileFile(

            // Policy source file.
            "policy.rego",

            // Entrypoints (same you would pass for -e parameter for opa build).
            new[] { "example/allow" }
            );

        // RegoCliCompiler will always produce bundle.
        using var engine = OpaEvaluatorFactory.CreateFromBundle(policy);

        #endregion
    }

    [Fact(Skip = "Documentation sample")]
    public async Task CompileBundle()
    {
        #region CompileBundle

        var compiler = new RegoCliCompiler();

        var policy = await compiler.CompileBundle(

            // Directory with bundle sources.
            "bundleDirectory",

            // Entrypoints (same you would pass for -e parameter for opa build).
            new[] { "example/allow" }
            );

        // RegoCliCompiler will always produce bundle.
        using var engine = OpaEvaluatorFactory.CreateFromBundle(policy);

        #endregion
    }

    [Fact(Skip = "Documentation sample")]
    public async Task CompileSource()
    {
        #region CompileSource

        var compiler = new RegoCliCompiler();

        var policySource = """
            package example

            import future.keywords.if

            default allow := false

            allow if {
                data.password == input.password
            }
            """;

        var policy = await compiler.CompileSource(

            // Policy source code.
            policySource,

            // Entrypoints (same you would pass for -e parameter for opa build).
            new[] { "example/allow" }
            );

        // RegoCliCompiler will always produce bundle.
        using var engine = OpaEvaluatorFactory.CreateFromBundle(policy);

        #endregion
    }
}