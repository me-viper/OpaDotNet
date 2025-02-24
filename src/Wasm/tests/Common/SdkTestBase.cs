using System.Text.Encodings.Web;
using System.Text.Json.Nodes;

using OpaDotNet.Wasm.Builtins;

namespace OpaDotNet.Wasm.Tests.Common;

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

        public JsonNode Expected { get; [UsedImplicitly] set; } = null!;

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
            import rego.v1

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
            SignatureValidation = new() { Validation = SignatureValidationType.Skip },
        };

        using var eval = await Build(src, "sdk", options: opts);
        return eval.Evaluate<object?, T>(null, "sdk").Result;
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
            SignatureValidation = new() { Validation = SignatureValidationType.Skip },
        };

        using var eval = await Build(src, "sdk", options: opts);
        return eval.EvaluateValue(value, "sdk");
    }

    protected async Task<IOpaEvaluator> Build(
        string source,
        string entrypoint,
        IOpaImportsAbi? imports = null,
        WasmPolicyEngineOptions? options = null,
        List<Func<IOpaCustomBuiltins>>? customBuiltins = null)
    {
        var policy = await CompileSource(source, [entrypoint]);

        var engineOpts = options ?? new WasmPolicyEngineOptions
        {
            SerializationOptions = DefaultJsonOptions,
            SignatureValidation = new() { Validation = SignatureValidationType.Skip },
        };

        var imp = imports ?? new TestImportsAbi(Output);

        using var factory = new OpaBundleEvaluatorFactory(
            policy,
            engineOpts,
            new DefaultBuiltinsFactory(options, () => imp) { CustomBuiltins = customBuiltins ?? [] }
            );

        return factory.Create();
    }
}