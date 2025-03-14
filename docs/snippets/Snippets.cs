﻿// ReSharper disable RedundantUsingDirective

#pragma warning disable CS0105


using OpaDotNet.Compilation.Abstractions;

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
        using var engine = OpaWasmEvaluatorFactory.Create(File.OpenRead("data/policy.wasm"));

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
        using var engine = OpaBundleEvaluatorFactory.Create(File.OpenRead("data/bundle.tar.gz"));

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

        var policy = await compiler.CompileFileAsync(

            // Policy source file.
            "quickstart/example.rego",
            new()
            {
                // Entrypoints (same you would pass for -e parameter for opa build).
                Entrypoints = ["example/hello"],
            }
            );

        // RegoCliCompiler will always produce bundle.
        using var engine = OpaBundleEvaluatorFactory.Create(policy);

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

        var policy = await compiler.CompileBundleAsync(

            // Directory with bundle sources.
            "quickstart",
            new()
            {
                // Entrypoints (same you would pass for -e parameter for opa build).
                Entrypoints = ["example/hello"],
            }
            );

        // RegoCliCompiler will always produce bundle.
        using var engine = OpaBundleEvaluatorFactory.Create(policy);

        #endregion

        var result = engine.EvaluatePredicate(new { message = "world" });
        Assert.True(result.Result);
    }

    [Fact]
    public async Task CompileSourceCli()
    {
        #region CompileSourceCli

        IRegoCompiler compiler = new RegoCliCompiler();

        var policySource = """
            package example

            default hello = false

            hello {
                x := input.message
                x == data.world
            }
            """;

        var policy = await compiler.CompileSourceAsync(

            // Policy source code.
            policySource,
            new()
            {
                // Entrypoints (same you would pass for -e parameter for opa build).
                Entrypoints = ["example/hello"],
            }
            );

        // RegoCliCompiler will always produce bundle.
        using var engine = OpaBundleEvaluatorFactory.Create(policy);

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

        var policy = await compiler.CompileFileAsync(

            // Policy source file.
            "quickstart/example.rego",
            new()
            {
                // Entrypoints (same you would pass for -e parameter for opa build).
                Entrypoints = ["example/hello"],
            }
            );

        // RegoCliCompiler will always produce bundle.
        using var engine = OpaBundleEvaluatorFactory.Create(policy);

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

        var policy = await compiler.CompileBundleAsync(

            // Directory with bundle sources.
            "quickstart/",
            new()
            {
                // Entrypoints (same you would pass for -e parameter for opa build).
                Entrypoints = ["example/hello"],
            }
            );

        // RegoCliCompiler will always produce bundle.
        using var engine = OpaBundleEvaluatorFactory.Create(policy);

        #endregion

        var result = engine.EvaluatePredicate(new { message = "world" });
        Assert.True(result.Result);
    }

    [Fact]
    public async Task CompileSourceInterop()
    {
        #region CompileSourceInterop

        IRegoCompiler compiler = new RegoInteropCompiler();

        var policySource = """
            package example

            default hello = false

            hello {
                x := input.message
                x == data.world
            }
            """;

        var policy = await compiler.CompileSourceAsync(

            // Policy source code.
            policySource,
            new()
            {
                // Entrypoints (same you would pass for -e parameter for opa build).
                Entrypoints = ["example/hello"],
            }
            );

        // RegoCliCompiler will always produce bundle.
        using var engine = OpaBundleEvaluatorFactory.Create(policy);

        #endregion

        engine.SetDataFromRawJson("""{ "world": "world" }""");

        var result = engine.EvaluatePredicate(new { message = "world" });
        Assert.True(result.Result);
    }
}