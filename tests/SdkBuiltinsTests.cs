﻿using System.Text.Json.Nodes;

using Microsoft.Extensions.Options;

using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class SdkBuiltinsTests
{
    private readonly ILoggerFactory _loggerFactory;

    private readonly ITestOutputHelper _output;

    private readonly OptionsWrapper<RegoCliCompilerOptions> _options =
        new(new());

    public SdkBuiltinsTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
    }

    [Theory]
    [InlineData("indexof_n(\"sad\", \"a\")", "[1]")]
    [InlineData("indexof_n(\"sadad\", \"a\")", "[1, 3]")]
    [InlineData("indexof_n(\"sad\", \"x\")", "[]")]
    public async Task StringsIndexOfN(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("sprintf(\"%s\", [\"hi!\"])", "\"hi!\"")]
    public async Task Sprintf(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("strings.any_prefix_match([\"aaa\", \"bbb\", \"ccc\"], [\"bb\"])", "true")]
    [InlineData("strings.any_prefix_match([\"aaa\", \"bbb\", \"ccc\"], [\"xx\", \"yy\", \"cc\"])", "true")]
    [InlineData("strings.any_prefix_match([\"aaa\", \"bbb\", \"ccc\"], [\"xx\"])", "false")]
    public async Task StringsAnyPrefixMatch(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    [Fact]
    public async Task StringsAnySuffixMatch()
    {
        var src = """
package sdk
t1 := o { o := strings.any_suffix_match(["saaa", "sbbb", "sccc"], ["bb"]) }
t2 := o { o := strings.any_suffix_match(["saaa", "sbbb", "sccc"], ["xx", "yy", "cc"]) }
t3 := o { o := strings.any_suffix_match(["saaa", "sbbb", "sccc"], ["xx"]) }
""";

        using var eval = await Build(src, "sdk");
        var result = eval.EvaluateValue(new { t1 = false, t2 = false, t3 = false }, "sdk");

        Assert.True(result.t1);
        Assert.True(result.t2);
        Assert.False(result.t3);
    }

    private class TimeImports : DefaultOpaImportsAbi
    {
        protected override DateTimeOffset Now()
        {
            return new DateTimeOffset(2023, 6, 5, 14, 27, 39, TimeSpan.Zero);
        }
    }

    [Theory]
    [InlineData("time.add_date(1672575347000000000, 1, 2, 3)", "1709554547000000000")]
    [InlineData("time.clock(1709554547000000000)", "[12, 15, 47]")]
    [InlineData("time.date(1709554547000000000)", "[2024, 3, 4]")]
    [InlineData("time.now_ns()", "1685975259000000000")]
    [InlineData("time.diff(1687527385064073200, 1672575347000000000)", "[0, 5, 22, 1, 20, 38]")]
    [InlineData("time.weekday(1687527385064073200)", "\"Friday\"")]
    public async Task Time(string func, string expected)
    {
        var result = await RunTestCase(func, expected, new TimeImports());
        Assert.True(result.Assert);
    }

    [Fact]
    public async Task UuidRfc4122()
    {
        var src = """
package sdk
t1 := o { o := uuid.rfc4122("k1") }
t2 := o { o := uuid.rfc4122("k2") }
t3 := o { o := uuid.rfc4122("k1") }
""";
        using var eval = await Build(src, "sdk");

        var result = eval.EvaluateValue(
            new { t1 = Guid.Empty, t2 = Guid.Empty, t3 = Guid.Empty },
            "sdk"
            );

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.t1);
        Assert.NotEqual(Guid.Empty, result.t2);
        Assert.NotEqual(Guid.Empty, result.t3);

        Assert.Equal(result.t1, result.t3);
        Assert.NotEqual(result.t1, result.t2);
    }

    [Fact]
    public async Task RandIntN()
    {
        var src = """
package sdk
t1 := o { o := rand.intn("k1", 1000) }
t2 := o { o := rand.intn("k2", 1000) }
t3 := o { o := rand.intn("k1", 1000) }
""";
        using var eval = await Build(src, "sdk");

        var result = eval.EvaluateValue(
            new { t1 = -1, t2 = -1, t3 = -1 },
            "sdk"
            );

        Assert.NotNull(result);
        Assert.NotEqual(-1, result.t1);
        Assert.NotEqual(-1, result.t2);
        Assert.NotEqual(-1, result.t3);

        Assert.Equal(result.t1, result.t3);
    }

    [Theory]
    [InlineData(
        """
net.cidr_contains_matches(["127.0.0.64/24", "10.0.0.64/24"], ["127.0.0.64/26", "127.0.0.1", "10.0.0.100", "1.0.0.1"])
""", "{[0, 0], [0, 1], [1, 2]}"
        )]
    [InlineData("""net.cidr_contains_matches("1.1.1.0/24", "1.1.1.128")""", """{["1.1.1.0/24", "1.1.1.128"]}""")]
    [InlineData("""net.cidr_contains_matches(["1.1.1.0/24", "1.1.2.0/24"], "1.1.1.128")""", """{[0, "1.1.1.128"]}""")]
    [InlineData(
        """
net.cidr_contains_matches([["1.1.0.0/16", "foo"], "1.1.2.0/24"], ["1.1.1.128", ["1.1.254.254", "bar"]])
""", """{[0, 0], [0, 1]}"""
        )]
    [InlineData(
        """
net.cidr_contains_matches([["1.1.0.0/16", "foo", 1], "1.1.2.0/24"], {"x": "1.1.1.128", "y": ["1.1.254.254", "bar"]})
""", """{[0, "x"], [0, "y"]}"""
        )]
    [InlineData(
        """
net.cidr_contains_matches([["1.1.2.0/24", "foo", 1], "1.1.0.0/16"], {"x": "1.1.1.128", "y": ["1.1.254.254", "bar"]})
""", """{[1, "x"], [1, "y"]}"""
        )]
    public async Task NetCidrContainsMatches(string func, string expected)
    {
        var result = await RunTestCase(func, expected, new TimeImports());
        Assert.True(result.Assert);
    }

    private const string NetCidrContainsMatchesTupleObjectsCase1 = """
t1 := o { o := net.cidr_contains_matches({["1.1.0.0/16", "foo", 1], "1.1.2.0/24"}, {"x": "1.1.1.128", "y": ["1.1.254.254", "bar"]}) }
""";

    private const string NetCidrContainsMatchesTupleObjectsCase2 = """
t1 := o { o := net.cidr_contains_matches({["1.1.2.0/24", "foo", 1], "1.1.0.0/16"}, {"x": "1.1.1.128", "y": ["1.1.254.254", "bar"]}) }
""";

    [Theory(Skip = "Don't know how to deduce set type")]
    [InlineData(NetCidrContainsMatchesTupleObjectsCase1, 1)]
    [InlineData(NetCidrContainsMatchesTupleObjectsCase2, 1)]
    public async Task NetCidrContainsMatchesTupleObjects(string s, int expectedIndex)
    {
        var result = await BuildAndEvaluate(
            s,
            new { t1 = Array.Empty<JsonArray>() }
            );

        Assert.NotNull(result.t1);
        Assert.Collection(
            result.t1,
            p => Assert.Collection(
                p,
                pp => Assert.Equal(expectedIndex, pp!.GetValue<int>()),
                pp => Assert.Equal("y", pp!.GetValue<string>())
                ),
            p => Assert.Collection(
                p,
                pp => Assert.Equal(expectedIndex, pp!.GetValue<int>()),
                pp => Assert.Equal("x", pp!.GetValue<string>())
                )
            );
    }

    [Theory]
    [InlineData("net.cidr_expand(\"192.168.0.0/30\")", "{\"192.168.0.0\", \"192.168.0.1\", \"192.168.0.2\", \"192.168.0.3\"}")]
    [InlineData("net.cidr_is_valid(\"192.168.0.0/30\")", "true")]
    [InlineData("net.cidr_is_valid(\"192.168.0.500/30\")", "false")]
    public async Task Net(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    [Fact]
    public async Task NetLookupIP()
    {
        var src = """
package sdk
t1 := o { o := net.lookup_ip_addr("google.com") }
t2 := o { o := net.lookup_ip_addr("bing.com1") }
""";
        using var eval = await Build(src, "sdk");

        var result = eval.EvaluateValue(
            new
            {
                t1 = Array.Empty<string>(),
                t2 = Array.Empty<string>(),
            },
            "sdk"
            );

        // Assert.Collection(
        //     result.t1,
        //     p => Assert.Equal("142.251.215.238", p),
        //     p => Assert.Equal("2607:f8b0:400a:80b::200e", p)
        //     );

        Assert.NotNull(result.t1);
        Assert.Null(result.t2);
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private record TestCaseResult
    {
        public bool Assert { get; set; }
        public JsonNode Expected { get; set; } = default!;
        public JsonNode? Actual { get; set; }
    }

    private async Task<TestCaseResult> RunTestCase(string actual, string expected, IOpaImportsAbi? imports = null)
    {
        var src = $$"""
package sdk
import future.keywords.if

assert if {
    expected == actual  
}
expected := r { r := {{expected}} }
actual := r { r := {{actual}} }
""";

        _output.WriteLine(src);
        _output.WriteLine("");

        using var eval = await Build(src, "sdk", imports);
        var result = eval.Evaluate<object?, TestCaseResult>(null);

        _output.WriteLine("");
        _output.WriteLine($"Expected:\n {result.Result.Expected}");
        _output.WriteLine($"Actual:\n {result.Result.Actual}");

        return result.Result;
    }

    private async Task<T> BuildAndEvaluate<T>(
        string statement,
        T value) where T : notnull
    {
        var src = $"""
package sdk
{statement}
""";
        using var eval = await Build(src, "sdk");
        return eval.EvaluateValue(value, "sdk");
    }

    private async Task<IOpaEvaluator> Build(
        string source,
        string entrypoint,
        IOpaImportsAbi? imports = null)
    {
        var compiler = new RegoCliCompiler(_options, _loggerFactory.CreateLogger<RegoCliCompiler>());
        var policy = await compiler.Compile(source, entrypoint);
        var factory = new OpaEvaluatorFactory(imports);

        var engineOpts = new WasmPolicyEngineOptions
        {
            SerializationOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            },
        };

        return factory.CreateFromBundle(policy, engineOpts);
    }
}