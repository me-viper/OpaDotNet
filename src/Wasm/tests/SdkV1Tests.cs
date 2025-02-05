using System.Collections;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Json.More;

using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.InternalTesting;
using OpaDotNet.Wasm.Builtins;
using OpaDotNet.Wasm.GoCompat;
using OpaDotNet.Wasm.Internal;
using OpaDotNet.Wasm.Rego;
using OpaDotNet.Wasm.Tests.Common;

using Yaml2JsonNode;

namespace OpaDotNet.Wasm.Tests;

public class SdkV1TestCase
{
    public string FileName { get; set; } = null!;

    public string Note { get; set; } = null!;

    public string Query { get; set; } = null!;

    public string[] Modules { get; set; } = null!;

    public JsonArray? WantResult { get; set; }

    public string? WantErrorCode { get; set; }

    public string? WantError { get; set; }

    public bool StrictError { get; set;}

    public JsonNode? Data { get; set; }

    public JsonNode? Input { get; set; }

    public JsonValue? InputTerm { get; set; }

    public bool SortBindings { get; set; }

    public override string ToString()
    {
        return $"{FileName} => {Note}";
    }
}

internal class SdkV1TestCaseContainer
{
    public SdkV1TestCase[] Cases { get; set; } = null!;
}

internal class SdkV1TestData : IEnumerable<object[]>
{
    private static readonly JsonSerializerOptions Opts = new(JsonSerializerOptions.Default)
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static readonly string BasePath = Path.Combine("TestData", "v1");

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<object[]> GetEnumerator()
    {
        var ignore = YamlSerializer.Deserialize<List<string>>(File.ReadAllText(Path.Combine(BasePath, "ignore.yaml")))!;

        var v1dir = Directory.EnumerateFiles(BasePath, "*.yaml", SearchOption.AllDirectories);

        foreach (var file in v1dir)
        {
            if (string.Equals(Path.GetFileName(file), "ignore.yaml", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var tc in ParseFile(file, ignore))
                yield return [tc];
        }
    }

    public static IEnumerable<SdkV1TestCase> ParseFile(string file, List<string> ignore)
    {
        var text = File.ReadAllText(file);
        var testCases = YamlSerializer.Deserialize<SdkV1TestCaseContainer>(text, Opts);

        if (testCases == null)
            yield break;

        foreach (var testCase in testCases.Cases)
        {
            testCase.FileName = file.Replace(BasePath + "\\", string.Empty);

            if (ignore.Any(ign => Regex.IsMatch(testCase.Note, ign)))
                continue;

            yield return testCase;
        }
    }
}

public class SdkV1Tests : SdkTestBase
{
    public SdkV1Tests(ITestOutputHelper output) : base(output)
    {
        Options = new()
        {
            CapabilitiesVersion = Utils.DefaultCapabilities,
            RegoVersion = RegoVersion.V1,
        };
    }

    [Theory]
    //[InlineData("cryptohmacsha1\\test-cryptohmacsha1.yaml")]
    //[InlineData("cryptohmacequal\\test-cryptohmacequal.yaml")]
    //[InlineData("semvercompare\\test-semvercompare-0347.yaml")]
    //[InlineData("globsmatch\\test-globsmatch-0866.yaml")]
    //[InlineData("disjunction\\test-disjunction-0775.yaml")]
    //[InlineData("containskeyword\\test-contains-future-keyword.yaml")]
    //[InlineData("eqexpr\\test-eqexpr-0599.yaml")]
    //         Do(testCase: strings\test-strings-0920.yaml => strings/sprintf: float)

    [InlineData("strings\\test-strings-0920.yaml")]
    public async Task DoCase(string fileName)
    {
        foreach (var tc in SdkV1TestData.ParseFile(Path.Combine(SdkV1TestData.BasePath, fileName), []))
        {
            await XDoRunTestCase(tc);
        }
    }

    [Theory]
    [InlineData("1677-09-21T00:12:43.145224192-00:00", -9223372036854775808)]
    [InlineData("2262-04-11T23:47:16.854775807-00:00", 9223372036854775807)]
    public void ParseNsLong(string s, long e)
    {
        var result = DateTimeExtensions.ParseRfc3339Ns(s);
        Assert.Equal(e, result);
    }

    //         Do(testCase: time\test-time-0948.yaml => time/parse_nanos_too_large)
    //         Do(testCase: time\test-time-0948.yaml => time/parse_nanos_too_small)
    [Theory]
    [InlineData("1677-09-21T00:12:43.145224191-00:00")]
    // [InlineData("1677-09-21T00:12:43.145224191-00:00")]
    // [InlineData("2262-04-11T23:47:16.854775807-00:00")]
    public void ParseNs(string s)
    {
        Assert.Throws<OpaBuiltinException>(() => DateTimeExtensions.ParseRfc3339Ns(s));
        // TimeParseNs
        // var result = DateTimeExtensions.Par(s);
        // Assert.Equal(e, result);
    }

    [Fact]
    public void Compare()
    {
        var a = """{"x":[[{"b":["a"]}],[{"b":["a","c"]}]]}""";
        var b = """{"x":[[{"b":["c","a"]}],[{"b":["a"]}]]}""";

        var ja = JsonNode.Parse(a);
        var jb = JsonNode.Parse(b);

        Assert.True(ja.IsEquivalentTo(jb, false));
    }

    [Theory]
    //[InlineData("cryptohmacsha1\\test-cryptohmacsha1.yaml")]
    //[InlineData("cryptohmacequal\\test-cryptohmacequal.yaml")]
    //[InlineData("semvercompare\\test-semvercompare-0347.yaml")]
    //[InlineData("globsmatch\\test-globsmatch-0866.yaml")]
    //[InlineData("disjunction\\test-disjunction-0775.yaml")]
    //[InlineData("containskeyword\\test-contains-future-keyword.yaml")]
    [InlineData("withkeyword\\test-withkeyword-1040.yaml")]
    public async Task DoCase2(string fileName)
    {
        foreach (var tc in SdkV1TestData.ParseFile(Path.Combine(SdkV1TestData.BasePath, fileName), []))
        {
            await XDoRunTestCase(tc);
        }
    }

    private async Task XDoRunTestCase(SdkV1TestCase testCase)
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

        var queryParts = testCase.Query.Split('=', StringSplitOptions.TrimEntries);

        if (queryParts.Length != 2)
            throw new NotSupportedException($"Invalid test case: {testCase}");

        var src = $$"""
            package run_test
            {{queryParts[1]}} := {{queryParts[0]}}
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

        var result = eval.Evaluate<object?, JsonNode>(testCase.Input);

        var expected = testCase.WantResult?.Count > 0 ? testCase.WantResult[0]! : new JsonObject();

        Output.WriteLine("expected:");
        Output.WriteLine(expected.ToJsonString());
        Output.WriteLine("actual:");
        Output.WriteLine(result.Result.ToJsonString());
        Output.WriteLine("-------");

        Assert.True(result.Result.IsEquivalentTo(expected, false));
    }

    [Theory]
    [ClassData(typeof(SdkV1TestData))]
    public async Task Do(SdkV1TestCase testCase)
    {
        await XDoRunTestCase(testCase);
        //Assert.True(result.Assert);
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
            options,
            new DefaultBuiltinsFactory(options, () => imp) { CustomBuiltins = customBuiltins ?? [] }
            );

        return factory.Create();
    }
}