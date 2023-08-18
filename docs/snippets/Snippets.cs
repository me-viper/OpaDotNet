// ReSharper disable RedundantUsingDirective

#pragma warning disable CS0105

#region Usings

using OpaDotNet.Wasm;

#endregion

#region CompilationCliUsings

using OpaDotNet.Wasm;
using OpaDotNet.Compilation.Cli;

#endregion

#region CompilationInteropUsings

using OpaDotNet.Wasm;
using OpaDotNet.Compilation.Interop;

#endregion

#pragma warning restore CS0105

namespace Snippets;

public partial class DocSamples
{
    [Fact]
    public void EvalWasm()
    {
        #region EvalWasm

        // Create evaluator from compiled policy module.
        using var engine = OpaEvaluatorFactory.CreateFromWasm(File.OpenRead("data/policy.wasm"));

        // Set external data.
        var data = """{ "world": "world" }""";
        engine.SetDataFromRawJson(data);

        // Evaluate. Policy query will return false.
        var deny = engine.EvaluatePredicate(new { message = "hi" });

        if (deny.Result)
        {
            // Should not get here.
        }
        else
        {
            // Wrong password.
        }

        // Evaluate. Policy query will return true.
        var approve = engine.EvaluatePredicate(new { message = "world" });

        if (approve.Result)
        {
            // Correct password.
        }
        else
        {
            // Should not get here.
        }

        #endregion

        Assert.False(deny.Result);
        Assert.True(approve.Result);
    }

    [Fact]
    public void EvalBundle()
    {
        #region EvalBundle

        // Create evaluator from compiled policy module.
        using var engine = OpaEvaluatorFactory.CreateFromBundle(File.OpenRead("data/bundle.tar.gz"));

        // External data is in the bundle already.

        // Evaluate. Policy query will return false.
        var deny = engine.EvaluatePredicate(new { message = "hi" });

        if (deny.Result)
        {
            // Should not get here.
        }
        else
        {
            // Wrong password.
        }

        // Evaluate. Policy query will return true.
        var approve = engine.EvaluatePredicate(new { message = "world" });

        if (approve.Result)
        {
            // Correct password.
        }
        else
        {
            // Should not get here.
        }

        #endregion

        Assert.False(deny.Result);
        Assert.True(approve.Result);
    }

    [Fact]
    public async Task CompileFileCli()
    {
        #region CompileFileCli

        var compiler = new RegoCliCompiler();

        var policy = await compiler.CompileFile(

            // Policy source file.
            "quickstart/example.rego",

            // Entrypoints (same you would pass for -e parameter for opa build).
            new[] { "example/hello" }
            );

        // RegoCliCompiler will always produce bundle.
        using var engine = OpaEvaluatorFactory.CreateFromBundle(policy);

        #endregion

        engine.SetDataFromRawJson("""{ "world": "world" }""");

        var result = engine.EvaluatePredicate(new { message = "world" });
        Assert.True(result.Result);
    }

    [Fact]
    public async Task CompileBundleCli()
    {
        #region CompileBundleCli

        var compiler = new RegoCliCompiler();

        var policy = await compiler.CompileBundle(

            // Directory with bundle sources.
            "quickstart/",

            // Entrypoints (same you would pass for -e parameter for opa build).
            new[] { "example/hello" }
            );

        // RegoCliCompiler will always produce bundle.
        using var engine = OpaEvaluatorFactory.CreateFromBundle(policy);

        #endregion

        var result = engine.EvaluatePredicate(new { message = "world" });
        Assert.True(result.Result);
    }

    [Fact]
    public async Task CompileSourceCli()
    {
        #region CompileSourceCli

        var compiler = new RegoCliCompiler();

        var policySource = """
            package example

            default hello = false

            hello {
                x := input.message
                x == data.world
            }
            """;

        var policy = await compiler.CompileSource(

            // Policy source code.
            policySource,

            // Entrypoints (same you would pass for -e parameter for opa build).
            new[] { "example/hello" }
            );

        // RegoCliCompiler will always produce bundle.
        using var engine = OpaEvaluatorFactory.CreateFromBundle(policy);

        #endregion

        engine.SetDataFromRawJson("""{ "world": "world" }""");

        var result = engine.EvaluatePredicate(new { message = "world" });
        Assert.True(result.Result);
    }

    [Fact]
    public async Task CompileFileInterop()
    {
        #region CompileFileInterop

        var compiler = new RegoInteropCompiler();

        var policy = await compiler.CompileFile(

            // Policy source file.
            "quickstart/example.rego",

            // Entrypoints (same you would pass for -e parameter for opa build).
            new[] { "example/hello" }
            );

        // RegoCliCompiler will always produce bundle.
        using var engine = OpaEvaluatorFactory.CreateFromBundle(policy);

        #endregion

        engine.SetDataFromRawJson("""{ "world": "world" }""");

        var result = engine.EvaluatePredicate(new { message = "world" });
        Assert.True(result.Result);
    }

    [Fact]
    public async Task CompileBundleInterop()
    {
        #region CompileBundleInterop

        var compiler = new RegoInteropCompiler();

        var policy = await compiler.CompileBundle(

            // Directory with bundle sources.
            "quickstart/",

            // Entrypoints (same you would pass for -e parameter for opa build).
            new[] { "example/hello" }
            );

        // RegoCliCompiler will always produce bundle.
        using var engine = OpaEvaluatorFactory.CreateFromBundle(policy);

        #endregion

        var result = engine.EvaluatePredicate(new { message = "world" });
        Assert.True(result.Result);
    }

    [Fact]
    public async Task CompileSourceInterop()
    {
        #region CompileSourceInterop

        var compiler = new RegoInteropCompiler();

        var policySource = """
            package example

            default hello = false

            hello {
                x := input.message
                x == data.world
            }
            """;

        var policy = await compiler.CompileSource(

            // Policy source code.
            policySource,

            // Entrypoints (same you would pass for -e parameter for opa build).
            new[] { "example/hello" }
            );

        // RegoCliCompiler will always produce bundle.
        using var engine = OpaEvaluatorFactory.CreateFromBundle(policy);

        #endregion

        engine.SetDataFromRawJson("""{ "world": "world" }""");

        var result = engine.EvaluatePredicate(new { message = "world" });
        Assert.True(result.Result);
    }
}