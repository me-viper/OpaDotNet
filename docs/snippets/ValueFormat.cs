// ReSharper disable RedundantUsingDirective

#pragma warning disable CS0105

#region ValueFormatUsings

using System.Text.Json.Nodes;

using OpaDotNet.Wasm;

#endregion

#pragma warning restore CS0105

using System.Text;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;

namespace Snippets;

public partial class DocSamples
{
    #region ValueFormatBuiltin

    public class ValueFormatBuiltins : DefaultOpaImportsAbi
    {
        public override object? Func(BuiltinContext context, BuiltinArg arg1)
        {
            if (string.Equals("custom.describe", context.FunctionName, StringComparison.Ordinal))
            {
                // With RegoValueFormat.Json (default) both {"a", "b"} and ["a", "b"] arrive as ["a","b"].
                // With RegoValueFormat.Value the set arrives as [{"__rego_set":["a","b"]}].
                var value = arg1.As<JsonNode>(RegoValueFormat.Value);
                var isSet = value is JsonArray { Count: 1 } array
                    && array[0] is JsonObject obj
                    && obj.ContainsKey("__rego_set");

                return isSet ? "set" : "not a set";
            }

            return base.Func(context, arg1);
        }
    }

    #endregion

    [Fact]
    public async Task ValueFormat()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var capabilities = """
            {
                "builtins": [
                    {
                        "name": "custom.describe",
                        "decl": { "type": "function", "args": [{ "type": "any" }], "result": { "type": "any" } }
                    }
                ]
            }
            """;

        var policySource = """
            package example

            set := custom.describe({"a", "b"})

            array := custom.describe(["a", "b"])
            """;

        IRegoCompiler compiler = new RegoCliCompiler();

        await using var policy = await compiler.CompileSourceAsync(
            policySource,
            new()
            {
                CapabilitiesVersion = "v0.53.1",
                CapabilitiesBytes = Encoding.UTF8.GetBytes(capabilities),
                Entrypoints = ["example/set", "example/array"],
            },
            cancellationToken
            );

        var options = new WasmPolicyEngineOptions();
        options.ConfigureBuiltins(p => p.DefaultBuiltins = new ValueFormatBuiltins());

        using var engine = OpaBundleEvaluatorFactory.Create(policy, options);

        Assert.Equal("set", engine.Evaluate<object, string>(new { }, "example/set").Result);
        Assert.Equal("not a set", engine.Evaluate<object, string>(new { }, "example/array").Result);
    }
}
