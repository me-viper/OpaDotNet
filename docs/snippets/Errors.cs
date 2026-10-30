// ReSharper disable RedundantUsingDirective

#pragma warning disable CS0105

#region ErrorsUsings

using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Validation;

#endregion

#pragma warning restore CS0105

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;

namespace Snippets;

public partial class DocSamples
{
    #region LoadPolicyErrors

    private static IOpaEvaluator? TryLoadPolicy(string bundlePath)
    {
        try
        {
            using var bundle = File.OpenRead(bundlePath);
            return OpaBundleEvaluatorFactory.Create(bundle);
        }
        catch (BundleSignatureValidationException)
        {
            // Bundle signature is invalid or file checksums don't match it.
            // BundleChecksumValidationException is derived from this type.
            return null;
        }
        catch (OpaRuntimeException)
        {
            // Bundle can't be unpacked or doesn't contain a valid OPA WASM module.
            return null;
        }
    }

    #endregion

    #region EvaluationErrors

    private static bool IsAllowed(IOpaEvaluator engine, object input, string entrypoint)
    {
        try
        {
            return engine.EvaluatePredicate(input, entrypoint).Result;
        }
        catch (OpaEvaluationException)
        {
            // Unknown entrypoint, timeout, memory limit, policy runtime error
            // or built-in error (if StrictBuiltinErrors is enabled).
            // The evaluator remains usable for subsequent evaluations.
            return false;
        }
    }

    #endregion

    [Fact]
    public void HandleErrors()
    {
        using var notBundle = TryLoadPolicy("data/policy.wasm");
        Assert.Null(notBundle);

        using var engine = TryLoadPolicy("data/bundle.tar.gz");
        Assert.NotNull(engine);

        Assert.False(IsAllowed(engine, new { message = "world" }, "example/missing"));
        Assert.True(IsAllowed(engine, new { message = "world" }, "example/hello"));
    }

    [Fact]
    public async Task UndefinedResults()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var policySource = """
            package example

            allow {
                input.user == "admin"
            }

            role := "admin" {
                input.user == "admin"
            }
            """;

        IRegoCompiler compiler = new RegoCliCompiler();

        await using var policy = await compiler.CompileSourceAsync(
            policySource,
            new() { Entrypoints = ["example/allow", "example/role"] },
            cancellationToken
            );

        using var engine = OpaBundleEvaluatorFactory.Create(policy);

        #region UndefinedResults

        var input = new { user = "bob" };

        // Undefined boolean result is reported as false.
        var allow = engine.EvaluatePredicate(input, "example/allow");

        // Undefined result is reported as default(TOutput), null here.
        var role = engine.EvaluateOrDefault<object, string>(input, "example/role");

        try
        {
            // Evaluate requires the result to be defined.
            engine.Evaluate<object, string>(input, "example/role");
        }
        catch (OpaEvaluationException)
        {
            // Policy evaluator returned empty result.
        }

        #endregion

        Assert.False(allow.Result);
        Assert.Null(role.Result);
        Assert.Throws<OpaEvaluationException>(() => engine.Evaluate<object, string>(input, "example/role"));
        Assert.Equal("[]", engine.EvaluateRaw("""{ "user": "bob" }""", "example/role"));
    }
}
