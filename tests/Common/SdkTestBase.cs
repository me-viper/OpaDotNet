﻿using System.Text.Encodings.Web;
using System.Text.Json.Nodes;

using JetBrains.Annotations;

using OpaDotNet.Wasm;

using Xunit.Abstractions;

namespace OpaDotNet.Tests.Common;

public class SdkTestBase(ITestOutputHelper output) : OpaTestBase(output)
{
    protected JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    protected record TestCaseResult
    {
        public bool Assert { get; [UsedImplicitly] set; }

        public JsonNode Expected { get; [UsedImplicitly] set; } = default!;

        public JsonNode? Actual { get; [UsedImplicitly] set; }
    }

    protected async Task<TestCaseResult> RunTestCase(
        string actual,
        string expected,
        bool fails = false,
        IOpaImportsAbi? imports = null,
        WasmPolicyEngineOptions? options = null)
    {
        var src = $$"""
            package sdk
            import future.keywords.if

            assert if {
                expected == actual
            }
            expected := {{expected}}
            actual := {{actual}}
            """;

        Output.WriteLine(src);
        Output.WriteLine("");

        using var eval = await Build(src, "sdk", imports, options);
        var result = eval.Evaluate<object?, TestCaseResult>(null);

        Output.WriteLine("");
        Output.WriteLine($"Expected:\n {result.Result.Expected}");
        Output.WriteLine($"Actual:\n {result.Result.Actual}");

        if (fails)
        {
            Assert.Null(result.Result.Actual);
            return new() { Assert = true };
        }

        return result.Result;
    }

    protected async Task<T> BuildAndEvaluate<T>(
        string statement,
        T value,
        bool strictErrors = false) where T : notnull
    {
        var src = $"""
            package sdk
            {statement}
            """;

        Output.WriteLine(src);
        Output.WriteLine("");

        var opts = new WasmPolicyEngineOptions
        {
            SerializationOptions = DefaultJsonOptions,
            StrictBuiltinErrors = strictErrors,
        };

        using var eval = await Build(src, "sdk", options: opts);
        return eval.EvaluateValue(value, "sdk");
    }

    protected async Task<IOpaEvaluator> Build(
        string source,
        string entrypoint,
        IOpaImportsAbi? imports = null,
        WasmPolicyEngineOptions? options = null)
    {
        var policy = await CompileSource(source, new[] { entrypoint });

        var engineOpts = options ?? new WasmPolicyEngineOptions { SerializationOptions = DefaultJsonOptions };

        var factory = new OpaBundleEvaluatorFactory(
            policy,
            engineOpts,
            importsAbiFactory: () => imports ?? new TestImportsAbi(Output)
            );

        return factory.Create();
    }
}