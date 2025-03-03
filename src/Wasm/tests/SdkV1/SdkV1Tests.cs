using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Nodes;
using System.Web;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.InternalTesting;
using OpaDotNet.Wasm.Builtins;
using OpaDotNet.Wasm.Internal;
using OpaDotNet.Wasm.Tests.Common;

namespace OpaDotNet.Wasm.Tests.SdkV1;

public partial class SdkV1Tests : SdkTestBase
{
    public SdkV1Tests(ITestOutputHelper output) : base(output)
    {
        Options = new()
        {
            CapabilitiesVersion = Utils.DefaultCapabilities,
            RegoVersion = RegoVersion.V1,
            CapabilitiesBytes = TestImportsAbi.Caps(),
        };
    }

    private void ApplyTestCaseShims(SdkV1TestCase testCase)
    {
        if (testCase.Note.StartsWith("reachable_paths/") && !string.IsNullOrWhiteSpace(testCase.InputTerm))
        {
            testCase.InputTerm = NormalizeSetInput(testCase.InputTerm);
            return;
        }

        if (testCase.Note.StartsWith("json_match_schema/invalid") || testCase.Note.StartsWith("json_verify_schema/invalid"))
        {
            testCase.Assert = (JsonNode e, JsonNode r) =>
            {
                var ear = e["x"]?.AsArray();
                var rar = r["x"]?.AsArray();

                Assert.NotNull(ear);
                Assert.NotNull(rar);
                Assert.True(ear.Count > 0);
                Assert.Equal(ear.Count, rar.Count);
                Assert.Equal(ear[0]!.GetValue<bool>(), rar[0]!.GetValue<bool>());
            };

            return;
        }

        if (string.Equals(testCase.Note, "urlbuiltins/encode_object strings", StringComparison.Ordinal))
        {
            testCase.Assert = (JsonNode e, JsonNode r) =>
            {
                var es = e["x"]?.GetValue<string>();
                var rs = r["x"]?.GetValue<string>();

                Assert.NotNull(es);
                Assert.NotNull(rs);

                var expected = HttpUtility.ParseQueryString(es);
                var result = HttpUtility.ParseQueryString(rs);

                Assert.Equal(expected.Count, result.Count);
                Assert.All(expected.AllKeys, p => Assert.Equal(expected.Get(p), result.Get(p)));
            };

            return;
        }

        if (string.Equals(testCase.Note, "cryptox509parsersaprivatekey/valid", StringComparison.Ordinal))
        {
            testCase.Assert = (JsonNode e, JsonNode r) =>
            {
                var ek = e["x"]?.AsObject();
                var rk = r["x"]?.AsObject();

                Assert.NotNull(ek);
                Assert.NotNull(rk);

                var keysToRemove = new List<string>();

                foreach (var x in rk)
                {
                    if (x.Value is JsonArray { Count: 0 })
                        keysToRemove.Add(x.Key);
                }

                foreach (var k in keysToRemove)
                    rk.Remove(k);

                Assert.True(ek.IsEquivalentTo(rk, false));
            };

            return;
        }

        if (testCase.Note.Equals("jwtencodesignraw/No Payload but Media Type is Plain")
            || testCase.Note.Equals("jwtencodesignraw/text/plain media type"))
        {
            return;
        }

        if (testCase.Note.StartsWith("jwtencodesign/") || testCase.Note.StartsWith("jwtencodesignraw/") || testCase.Note.StartsWith("set data"))
        {
            testCase.Assert = (JsonNode e, JsonNode r) =>
            {
                var ek = e["x"]?.GetValue<string>();
                var rk = r["x"]?.GetValue<string>();

                Assert.NotNull(ek);
                Assert.NotNull(rk);

                var handler = new JwtSecurityTokenHandler();
                var et = handler.ReadJwtToken(ek);
                var rt = handler.ReadJwtToken(rk);

                Assert.Equivalent(et.Header, rt.Header);
                Assert.Equivalent(et.Payload, rt.Payload);
            };

            return;
        }
    }

    [Theory]
    [InlineData(
        """{ "graph": { "a": ["b"], "b": ["c"], "c": ["a"], }, "initial": ["a"] }""",
        """{ "graph": { "a": ["b"], "b": ["c"], "c": ["a"]  }, "initial": ["a"] }"""
        )]
    public void NormalizeSetInputTest(string i, string e)
    {
        Assert.Equal(e, NormalizeSetInput(i));
    }

    private string NormalizeSetInput(ReadOnlySpan<char> input)
    {
        Span<char> result = stackalloc char[input.Length];
        input.CopyTo(result);

        for (var i = 0; i < input.Length; i++)
        {
            if (input[i] == ',')
            {
                var comaIndex = i;
                i++;

                for (; i < input.Length; i++)
                {
                    if (input[i] != ' ' && input[i] != '}')
                        break;

                    if (input[i] == '}')
                    {
                        result[comaIndex] = ' ';
                        break;
                    }
                }
            }
        }

        return result.ToString();
    }

    private async Task RunTestCase(SdkV1TestCase testCase)
    {
        var engineOpts = new WasmPolicyEngineOptions
        {
            SerializationOptions = DefaultJsonOptions,
            SignatureValidation = new() { Validation = SignatureValidationType.Skip },
            StrictBuiltinErrors = testCase.StrictError,
        };

        var shouldFail = false;
        var testSrc = new StringBuilder();

        if (testCase.WantResult == null)
        {
            if (string.IsNullOrWhiteSpace(testCase.WantErrorCode) && string.IsNullOrWhiteSpace(testCase.WantError))
                throw new NotSupportedException($"Invalid test case: {testCase}");

            shouldFail = true;
        }

        var srcParts = new StringBuilder();

        var queryParts = testCase.Query.Split(['=', ';'], StringSplitOptions.TrimEntries);

        if (queryParts.Length % 2 != 0)
            throw new NotSupportedException($"Invalid test case: {testCase}");

        for (var i = 0; i < queryParts.Length; i += 2)
            srcParts.AppendLine($"{queryParts[i + 1]} := {queryParts[i]}");

        var src = $$"""
            package run_test
            {{srcParts}}
            """;

        using var ms = new MemoryStream();

        await using (var bw = new BundleWriter(ms))
        {
            for (var i = 0; i < testCase.Modules.Length; i++)
            {
                bw.WriteEntry(testCase.Modules[i], $"m{i}.rego");
                testSrc.AppendLine(testCase.Modules[i]);
            }

            bw.WriteEntry(src, "assert.rego");
            testSrc.AppendLine(src);

            if (testCase.Data != null)
            {
                bw.WriteEntry(testCase.Data.ToJsonString(), "data.json");
                testSrc.AppendLine();
                testSrc.AppendLine("data:");
                testSrc.AppendLine(testCase.Data.ToJsonString());
            }
        }

        ms.Seek(0, SeekOrigin.Begin);

        if (testCase.Input != null)
        {
            testSrc.AppendLine();
            testSrc.AppendLine("input:");
            testSrc.AppendLine(testCase.Input.ToJsonString());
        }

        if (testCase.InputTerm != null)
        {
            testSrc.AppendLine();
            testSrc.AppendLine("input_term:");
            testSrc.AppendLine(testCase.InputTerm);
        }

        Output.WriteLine(testCase.Note);
        Output.WriteLine(testSrc.ToString());

        using var eval = await Build(ms, "run_test", null, engineOpts);

        Output.WriteLine("-------");

        if (shouldFail)
        {
            if (testCase.StrictError)
            {
                var ex = Assert.Throws<OpaEvaluationException>(() => eval.Evaluate<object?, JsonNode>(testCase.Input));

                Assert.NotNull(ex.InnerException);
                Assert.IsType<OpaBuiltinException>(ex.InnerException);

                var innerEx = (OpaBuiltinException)ex.InnerException;

                Assert.NotNull(innerEx.Name);

                if (!string.IsNullOrWhiteSpace(testCase.WantErrorCode))
                    Assert.Equal(testCase.WantErrorCode, innerEx.ErrorCode);

                Output.WriteLine("------- Exception as expected -------");
                Output.WriteLine(innerEx.ToString());
                Output.WriteLine("");

                return;
            }

            if (string.Equals("eval_conflict_error", testCase.WantErrorCode, StringComparison.Ordinal))
            {
                Assert.Throws<OpaEvaluationAbortedException>(() => eval.Evaluate<object?, JsonNode>(testCase.Input));
                return;
            }

            Assert.Fail("Don't know how to fail");
        }

        PolicyEvaluationResult<JsonNode> result;

        if (string.IsNullOrWhiteSpace(testCase.InputTerm))
            result = eval.Evaluate<object?, JsonNode>(testCase.Input);
        else
        {
            var rawResult = eval.EvaluateRaw(testCase.InputTerm);
            var evalResult = JsonSerializer.Deserialize<PolicyEvaluationResult<JsonNode>[]>(rawResult);

            Assert.NotNull(evalResult);
            Assert.Single(evalResult);
            result = evalResult[0];
        }

        var expected = testCase.WantResult?.Count > 0 ? testCase.WantResult[0]! : new JsonObject();

        Output.WriteLine("expected:");
        Output.WriteLine(expected.ToJsonString());
        Output.WriteLine("actual:");
        Output.WriteLine(result.Result.ToJsonString());
        Output.WriteLine("-------");

        if (testCase.Assert == null)
            Assert.True(result.Result.IsEquivalentTo(expected, false));
        else
            testCase.Assert(expected, result.Result);
    }

    private async Task<IOpaEvaluator> Build(
        Stream source,
        string entrypoint,
        IOpaImportsAbi? imports = null,
        WasmPolicyEngineOptions? options = null,
        List<Func<IOpaCustomBuiltins>>? customBuiltins = null)
    {
        var policy = await CompileBundle(source, [entrypoint]);

        var imp = imports ?? new TestImportsAbi(Output);

        using var factory = new OpaBundleEvaluatorFactory(
            policy,
            options
            );

        return factory.Create();
    }
}