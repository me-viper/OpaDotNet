using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;

using OpaDotNet.Wasm.Builtins;
using OpaDotNet.Wasm.Tests.Common;

// ReSharper disable StringLiteralTypo

namespace OpaDotNet.Wasm.Tests;

public class SdkBuiltinsTests(ITestOutputHelper output) : SdkTestBase(output)
{
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
    [InlineData("""sprintf("%s", ["hi!"])""", "\"hi!\"")]
    [InlineData("""sprintf("%s", [10])""", "\"10\"")]
    [InlineData(
        """sprintf("%s", [[1, "hi", 3]])""", """
        "[1, \"hi\", 3]"
        """
        )]
    [InlineData("""sprintf("%s", [{1,2,3}])""", "\"{1, 2, 3}\"")]

    // Not really compatible with how rego formats objects but close enough.
    [InlineData(
        """sprintf("%s", [{"a": 1, "b": "b", "c": true}])""", """
        "{\"c\":true,\"b\":\"b\",\"a\":1}"
        """
        )]
    [InlineData("""sprintf("%d", [10])""", "\"10\"")]
    [InlineData("""sprintf("%b", [3])""", "\"11\"")]
    [InlineData("""sprintf("%10b", [3])""", "\"        11\"")]
    [InlineData("""sprintf("%x", [11])""", "\"b\"")]
    [InlineData("""sprintf("%e", [123.456])""", "\"1.234560e+002\"")]
    [InlineData("""sprintf("%E", [123.456])""", "\"1.234560E+002\"")]
    [InlineData("""sprintf("%f", [123.456])""", "\"123.456000\"")]
    [InlineData("""sprintf("%F", [123.456])""", "\"123.456000\"")]
    [InlineData("""sprintf("%g", [123.456])""", "\"123.456\"")]
    [InlineData("""sprintf("%G", [123.456])""", "\"123.456\"")]
    [InlineData("""sprintf("%.0f", [123.456])""", "\"123\"")]
    [InlineData("""sprintf("%10.3f", [123.456])""", "\"   123.456\"")]
    [InlineData("""sprintf("%s %d", ["hi", 1])""", "\"hi 1\"")]
    public async Task Sprintf(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""sprintf("%s", ["hi&bye"])""", "\"hi&bye\"")]
    public async Task SprintfJsonEncoding(string func, string expected)
    {
        var opts = new WasmPolicyEngineOptions
        {
            SerializationOptions = new()
            {
                PropertyNameCaseInsensitive = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            }
        };

        var result = await RunTestCase(func, expected, options: opts);
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

    [Theory]
    [InlineData("""strings.any_suffix_match(["saaa", "sbbb", "sccc"], ["bb"])""", "true")]
    [InlineData("""strings.any_suffix_match(["saaa", "sbbb", "sccc"], ["xx", "yy", "cc"])""", "true")]
    [InlineData("""strings.any_suffix_match(["saaa", "sbbb", "sccc"], ["xx"])""", "false")]
    public async Task StringsAnySuffixMatch(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""strings.count("qqqq", "q")""", 4)]
    [InlineData("""strings.count("cheese", "e")""", 3)]
    [InlineData("""strings.count("hello hello hello world", "hello")""", 3)]
    [InlineData("""strings.count("dummy", "x")""", 0)]
    public async Task StringsCount(string func, int expected)
    {
        var result = await RunTestCase(func, expected.ToString());
        Assert.True(result.Assert);
    }

    private class TimeImports(ITestOutputHelper output) : TestImportsAbi(output)
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
        var result = await RunTestCase(func, expected, false, new TimeImports(Output));
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("time.format(1707133074028819500)", "\"2024-02-05T11:37:54.0288195Z\"")]
    [InlineData("""time.format([1707133074028819500, "America/New_York"])""", "\"2024-02-05T06:37:54.0288195-05:00\"")]
    [InlineData("""time.format([1707133074028819500, "EST", "RFC822"])""", "\"05 Feb 24 06:37 EST\"")]
    [InlineData("""time.format([1707133074028819500, "UTC", "06 01 02"])""", "\"24 02 05\"")]
    [InlineData("""time.format([1707133074028819500, "", "06 01 02"])""", "\"24 02 05\"")]
    public async Task TimeFormat(string func, string expected)
    {
        var result = await RunTestCase(func, expected, false, new TimeImports(Output));
        Assert.True(result.Assert);
    }

    [Fact]
    public async Task TimeFormatLocalTimeZone()
    {
        var localTz = TimeZoneInfo.Local;
        var func = """time.format([1707133074028819500, "Local", "-07"])""";
        var sign = localTz.BaseUtcOffset.Hours >= 0 ? "+" : "-";
        var expected = $"\"{sign}{localTz.BaseUtcOffset.Hours:00}\"";

        var result = await RunTestCase(func, expected, false, new TimeImports(Output));
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""time.parse_ns("Mon Jan _2 15:04:05.000000 2006", "Thu Feb  4 21:00:57,123450 2010")""", "1265317257123450000")]
    [InlineData("""time.parse_ns("RFC3339", "2010-02-04T21:00:57.12345Z")""", "1265317257123450000")]
    public async Task TimeParse(string func, string expected)
    {
        var result = await RunTestCase(func, expected, false, new TimeImports(Output));
        Assert.True(result.Assert);
    }

    private class TimeNowImports : TestImportsAbi
    {
        public TimeNowImports(ITestOutputHelper output) : base(output)
        {
            var now = (new DateTimeOffset(2023, 6, 5, 14, 27, 39, TimeSpan.Zero).Ticks - DateTimeOffset.UnixEpoch.Ticks) * 100;
            CacheGetOrAddValue("time.now_ns", () => now);
        }
    }

    [Theory]
    [InlineData("time.now_ns()", "1685975259000000000")]
    public async Task TimeNow(string func, string expected)
    {
        var result = await RunTestCase(func, expected, false, new TimeNowImports(Output));
        Assert.True(result.Assert);
    }

    [Fact]
    public async Task TimeNowIsUtc()
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var result = await RunTestCase("time.clock(time.now_ns())[0]", nowUtc.Hour.ToString());
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""time.parse_duration_ns("-10s")""", "-10000000000")]
    [InlineData("""time.parse_duration_ns("1µs")""", "1000")]
    [InlineData("""time.parse_duration_ns("1.5h")""", "5400000000000")]
    [InlineData("""time.parse_duration_ns("1ns")""", "1")]
    public async Task TimeParseDurationNs(string func, string expected)
    {
        var result = await RunTestCase(func, expected, false, new TimeImports(Output));
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""time.parse_rfc3339_ns("1985-04-12T23:20:50.52Z")""", "482196050520000000")]
    [InlineData("""time.parse_rfc3339_ns("1996-12-19T16:39:57-08:00")""", "851042397000000000")]
    [InlineData("""time.parse_rfc3339_ns("1990-12-31T23:59:59Z")""", "662687999000000000")]
    [InlineData("""time.parse_rfc3339_ns("1937-01-01T12:00:27.87+00:20")""", "-1041337172130000000")]
    public async Task TimeParseRfc3339Ns(string func, string expected)
    {
        var result = await RunTestCase(func, expected, false, new TimeImports(Output));
        Assert.True(result.Assert);
    }

    private class DebugImports : IOpaCustomBuiltins, IOpaCustomPrinter
    {
        public StringBuilder Output { get; } = new();

        public void Print(IEnumerable<string> args)
        {
            var str = args as string[] ?? args.ToArray();
            Output.AppendJoin(", ", str);
        }

        public void Reset()
        {
        }
    }

    [Theory]
    [InlineData("\"hi\"", "\"hi\"")]
    [InlineData("\"hi\", 1", "\"hi\", 1")]
    [InlineData("""{"a": 1, "b": "aaa"}""", """{"b":"aaa","a":1}""")]
    [InlineData("""[1,2,3]""", """[1,2,3]""")]
    public async Task Print(string args, string expected)
    {
        var src = $$"""
            package sdk

            t1 := o {
                print({{args}})
                o := true
            }
            """;

        var import = new DebugImports();
        using var eval = await Build(src, "sdk", customBuiltins: [() => import]);

        var result = eval.EvaluateValue(new { t1 = false }, "sdk");

        Assert.True(result.t1);
        Assert.Equal(expected, import.Output.ToString());
    }

    [Fact]
    public async Task Trace()
    {
        var src = """
            package sdk

            t1 := o {
                o := "hi!"
                trace(o)
            }
            """;
        var import = new DebugImports();
        using var eval = await Build(src, "sdk", customBuiltins: [() => import]);

        var result = eval.EvaluateValue(new { t1 = string.Empty }, "sdk");

        Assert.Equal("hi!", result.t1);
        Assert.Equal("hi!", import.Output.ToString());
    }

    [Fact]
    public async Task UuidRfc4122()
    {
        var src = """
            package sdk
            t1 := uuid.rfc4122("k1")
            t2 := uuid.rfc4122("k2")
            t3 := uuid.rfc4122("k1")
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

    [Theory]
    [InlineData("""uuid.parse("f47ac10b-58cc-4372-c567-0e02b2c3d479")""", """{"variant":"Microsoft","version":4}""")]
    [InlineData("""uuid.parse("f47ac10b-58cc-4372-b567-0e02b2c3d479")""", """{"variant":"RFC4122","version":4}""")]
    [InlineData("""uuid.parse("f47ac10b-58cc-4372-e567-0e02b2c3d479")""", """{"variant":"Future","version":4}""")]
    [InlineData("""uuid.parse("f47ac10b-58cc-7372-8567-0e02b2c3d479")""", """{"variant":"RFC4122","version":7}""")]
    [InlineData("""uuid.parse("f47ac10b-58cc-b372-8567-0e02b2c3d479")""", """{"variant":"RFC4122","version":11}""")]
    [InlineData("""uuid.parse("urn:uuid:f47ac10b-58cc-b372-8567-0e02b2c3d479")""", """{"variant":"RFC4122","version":11}""")]
    public async Task UuidParse(string func, string expected, bool fails = false)
    {
        var result = await RunTestCase(func, expected, fails);
        Assert.True(result.Assert);
    }

    [Fact]
    public async Task RandIntN()
    {
        var src = """
            package sdk
            t1 := rand.intn("k1", 1000)
            t2 := rand.intn("k2", 1000)
            t3 := rand.intn("k1", 1000)
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
    [InlineData(
        """
        net.cidr_contains_matches({["1.1.0.0/16", "foo", 1], "1.1.2.0/24"}, {"x": "1.1.1.128", "y": ["1.1.254.254", "bar"]})
        """, """{[["1.1.0.0/16", "foo", 1], "x"], [["1.1.0.0/16", "foo", 1], "y"]}"""
        )]
    [InlineData(
        """
        net.cidr_contains_matches({["1.1.2.0/24", "foo", 1], "1.1.0.0/16"}, {"x": "1.1.1.128", "y": ["1.1.254.254", "bar"]})
        """, """{["1.1.0.0/16", "x"], ["1.1.0.0/16", "y"]}"""
        )]
    public async Task NetCidrContainsMatchesObjects(string func, string expected)
    {
        var result = await RunTestCase(func, expected, false, new TimeImports(Output));
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""net.cidr_merge(["192.0.128.0/24", "192.0.129.0/24"])""", """{"192.0.128.0/23"}""")]
    [InlineData("net.cidr_is_valid(\"192.168.0.0/30\")", "true")]
    [InlineData("net.cidr_is_valid(\"192.168.0.500/30\")", "false")]
    [InlineData("""net.cidr_expand("192.168.0.0/30")""", """{"192.168.0.0", "192.168.0.1", "192.168.0.2", "192.168.0.3"}""")]
    public async Task Net(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""net.cidr_merge(["192.0.128.0/24", "192.0.129.0/24"])""", """{"192.0.128.0/23"}""")]
    [InlineData("""net.cidr_merge(["2001:0db8::/32", "2001:0db9::/32"])""", """{"2001:db8::/31"}""")]
    public async Task NetCidrMerge(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    [Fact]
    public async Task NetLookupIP()
    {
        var src = """
            package sdk
            t1 := net.lookup_ip_addr("google.com")
            t2 := net.lookup_ip_addr("bing.com1")
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

    [Theory]
    [InlineData("""base64url.encode_no_pad("message")""", "\"bWVzc2FnZQ\"")]
    [InlineData("""hex.decode("6d657373616765")""", "\"message\"")]
    [InlineData("""hex.encode("message")""", "\"6d657373616765\"")]
    [InlineData("""urlquery.encode("x=https://w.org/ref/#encoding")""", "\"x%3dhttps%3a%2f%2fw.org%2fref%2f%23encoding\"")]
    [InlineData("""urlquery.decode("x%3Dhttps%3A%2F%2Fw.org%2Fref%2F%23encoding")""", "\"x=https://w.org/ref/#encoding\"")]
    [InlineData(
        """urlquery.decode_object("e=1&e=2&d=b&d=a&c=true&b=bbb&a=1")""",
        """{"a": ["1"], "b": ["bbb"], "c": ["true"], "d": ["b", "a"], "e": ["1", "2"]}"""
        )]
    [InlineData(
        """urlquery.encode_object({"a": "1", "b": "bbb", "c": "true", "d": {"a", "b"}, "e": ["1", "2"]})""",
        "\"e=1&e=2&d=b&d=a&c=true&b=bbb&a=1\""
        )]
    [InlineData("""urlquery.encode_object({})""", "\"\"")]
    [InlineData("""urlquery.encode_object({"a": "b?b"})""", "\"a=b%3fb\"")]
    public async Task Encoding(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""regex.find_n("aa", "aaabbbaaaccc", 1)""", "[\"aa\"]")]
    [InlineData("""regex.find_n("aa", "aaabbbaaaccc", -1)""", "[\"aa\", \"aa\"]")]
    [InlineData("""regex.find_n("aa", "aaabbbaaaccc", 0)""", "[]")]
    [InlineData("""regex.find_n("aa", "aaabbbaaaccc", 5)""", "[\"aa\", \"aa\"]")]
    [InlineData("""regex.replace("aaabbbaaccca", "a+", "x")""", "\"xbbbxcccx\"")]
    [InlineData("""regex.split("a+", "aaabbbaaccca")""", "[\"\", \"bbb\", \"ccc\", \"\"]")]
    public async Task Regex(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""regex.template_match("urn:foo:{.*}", "urn:foo:bar:baz", "{", "}")""", "true")]
    [InlineData("""regex.template_match("urn:foo.bar.com:{.*}", "urn:foo.bar.com:bar:baz", "{", "}")""", "true")]
    [InlineData("""regex.template_match("urn:foo.bar.com:{.*}", "urn:foo.com:bar:baz", "{", "}")""", "false")]
    [InlineData("""regex.template_match("urn:foo.bar.com:{.*}", "foobar", "{", "}")""", "false")]
    [InlineData("""regex.template_match("urn:foo.bar.com:{.{1,2}}", "urn:foo.bar.com:aa", "{", "}")""", "true")]
    [InlineData("""regex.template_match("urn:foo.bar.com:{.*{}", "", "{", "}")""", "true", true)]
    [InlineData("""regex.template_match("urn:foo:<.*>", "urn:foo:bar:baz", "<", ">")""", "true")]
    public async Task RegexTemplateMatch(string func, string expected, bool fails = false)
    {
        var result = await RunTestCase(func, expected, fails);
        Assert.True(result.Assert);
    }

    [Fact]
    public async Task OpaRuntime()
    {
        var src = """
            package sdk
            r := opa.runtime()
            """;
        using var eval = await Build(src, "sdk");

        var result = eval.EvaluateValue(
            new { r = new JsonObject(), },
            "sdk"
            );

        Assert.NotNull(result.r);
        Output.WriteLine(result.r.ToString());
    }

    [Theory]
    [InlineData("""semver.is_valid("1.1.12-rc1+foo")""", "true")]
    [InlineData("""semver.is_valid("1.1.12-rc.1+foo")""", "true")]
    [InlineData("""semver.is_valid("v1.1.12-rc1+foo")""", "false")]
    [InlineData("""semver.is_valid(1)""", "false")]
    [InlineData("""semver.is_valid(["1.1.12-rc1+foo"])""", "false")]
    [InlineData("""semver.compare("1.1.12-rc1+foo", "1.1.12-rc1+foo")""", "0")]
    [InlineData("""semver.compare("1.1.12", "foo")""", "0", true)]
    [InlineData("""semver.compare("foo", "1.1.12")""", "0", true)]
    [InlineData("""semver.compare("1.2.12", "1.1.12")""", "1")]
    [InlineData("""semver.compare("1.1.12", "1.2.12")""", "-1")]
    public async Task Semver(string func, string expected, bool fails = false)
    {
        var result = await RunTestCase(func, expected, fails);
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""json.patch({"a": {"foo": 1}}, [{"op": "add", "path": "/a/bar", "value": 2}])""", """{"a": {"foo": 1, "bar": 2}}""")]
    public async Task JsonPatch(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""json.verify_schema({})""", "[true,null]")]
    [InlineData("""json.verify_schema("{}")""", "[true,null]")]
    [InlineData("""json.verify_schema({"a": {"foo": 1}})""", "[true,null]")]
    [InlineData("""json.verify_schema({"properties": { "id": { "type": "UNKNOWN" } }, "required": ["id"] })""", """[false,"Could not find appropriate value for UNKNOWN in type SchemaValueType"]""")]
    public async Task JsonVerifySchema(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""json.match_schema({}, {})""", "[true,[]]")]
    [InlineData("""json.match_schema({}, `{"a":"`)""", "[false,[]]", true)]
    [InlineData("""json.match_schema(`{"a":"`, {})""", "[false,[]]", true)]
    [InlineData("""json.match_schema({"id":5}, {"properties":{"id":{"type":"integer"}},"required":["id"]})""", "[true,[]]")]
    [InlineData("""json.match_schema(`{"id":5}`, {"properties":{"id":{"type":"integer"}},"required":["id"]})""", "[true,[]]")]
    [InlineData("""json.match_schema({"id":"test"}, {"properties":{"id":{"type":"integer"}},"required":["id"]})[0]""", "false")]
    public async Task JsonMatchSchema(string func, string expected, bool fails = false)
    {
        var result = await RunTestCase(func, expected, fails);
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""object.subset({"a": "b", "c": {"x": {10, 15, 20, 25}, "y": "z"}}, {"c": {"x": {10, 15, 20}}})""", "true")]
    [InlineData("""object.subset({"a": "b", "c": {"x": [10, 15, 20, 25]}, "y": "z"}, {"c": {"x": [10, 15, 20]}})""", "true")]
    [InlineData("""object.subset({"a": "b", "c": {"x": {10, 15, 20, 25}}, "y": "z"}, {"c": {"x": {10, 15, 20, 35}}})""", "false")]
    [InlineData("""object.subset({10, 15, 20, 25}, {25, 15, 10})""", "true")]
    [InlineData("""object.subset([10, 15, 20, 25], [10, 15])""", "true")]
    [InlineData("""object.subset([10, 15, 20, 25], {15, 10})""", "true")]
    [InlineData("""object.subset([{"a": 1}, {"b": "a"}], [{"b": "a"}])""", "true")]
    [InlineData("""object.subset([{"a": 1}, {"b": "a"}], {{"b": "a"}})""", "true")]
    [InlineData("""object.subset([1, 2, 1, 2, 3], [2, 3])""", "true")]
    [InlineData("""object.subset([1, 2, 1, 2, 3], [2, 3, 1])""", "false")]
    [InlineData("""object.subset({10, 15, 20, 25}, [15, 10])""", "false", true)]
    public async Task ObjectSubset(string func, string expected, bool fails = false)
    {
        var result = await RunTestCase(func, expected, fails);
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""units.parse_bytes("1.2")""", "1")]
    [InlineData("""units.parse_bytes("10")""", "10")]
    [InlineData("""units.parse_bytes("10.0")""", "10")]
    [InlineData("""units.parse_bytes("10K")""", "10000")]
    [InlineData("""units.parse_bytes("10KB")""", "10000")]
    [InlineData("""units.parse_bytes("10KI")""", "10240")]
    [InlineData("""units.parse_bytes("10KIB")""", "10240")]
    [InlineData("""units.parse_bytes("10M")""", "10000000")]
    [InlineData("""units.parse_bytes("10MI")""", "10485760")]
    [InlineData("""units.parse_bytes("10G")""", "10000000000")]
    [InlineData("""units.parse_bytes("10GI")""", "10737418240")]
    [InlineData("""units.parse_bytes("10T")""", "10000000000000")]
    [InlineData("""units.parse_bytes("10TB")""", "10000000000000")]
    [InlineData("""units.parse_bytes("10TI")""", "10995116277760")]
    [InlineData("""units.parse_bytes("10TIB")""", "10995116277760")]
    [InlineData("""units.parse_bytes("10P")""", "10000000000000000")]
    [InlineData("""units.parse_bytes("10PI")""", "11258999068426240")]
    [InlineData("""units.parse_bytes("10PIB")""", "11258999068426240")]
    [InlineData("""units.parse_bytes("10E")""", "10000000000000000000")]

    // Native implementation seems does rounding (result will be 11529215046068470000).
    [InlineData("""units.parse_bytes("10EI")""", "11529215046068469760")]
    [InlineData("""units.parse_bytes("10EIB")""", "11529215046068469760")]
    [InlineData("""units.parse_bytes("b")""", "0", true)]
    [InlineData("""units.parse_bytes(`1.2"ki"`)""", "1228")]
    [InlineData("""units.parse_bytes(`-1b`)""", "0", true)]
    public async Task UnitsParseBytes(string func, string expected, bool fails = false)
    {
        Output.WriteLine("Ordinal");
        var result = await RunTestCase(func, expected, fails);
        Assert.True(result.Assert);

        Output.WriteLine("Lower case");
        var lowerCase = await RunTestCase(func.ToLowerInvariant(), expected, fails);
        Assert.True(lowerCase.Assert);
    }

    [Theory]
    [InlineData("""units.parse("1.2m")""", "0.0012")]
    [InlineData("""units.parse("1.2")""", "1.2")]
    [InlineData("""units.parse("10")""", "10")]
    [InlineData("""units.parse("10.0")""", "10")]
    [InlineData("""units.parse("10.0k")""", "10000")]
    [InlineData("""units.parse("10K")""", "10000")]
    [InlineData("""units.parse("10KI")""", "10240")]
    [InlineData("""units.parse("10M")""", "10000000")]
    [InlineData("""units.parse("10MI")""", "10485760")]
    [InlineData("""units.parse("10G")""", "10000000000")]
    [InlineData("""units.parse("10GI")""", "10737418240")]
    [InlineData("""units.parse("10T")""", "10000000000000")]
    [InlineData("""units.parse("10TI")""", "10995116277760")]
    [InlineData("""units.parse("10P")""", "10000000000000000")]
    [InlineData("""units.parse("10PI")""", "11258999068426240")]
    [InlineData("""units.parse("10E")""", "10000000000000000000")]

    // Native implementation seems does rounding (result will be 11529215046068470000).
    [InlineData("""units.parse("10EI")""", "11529215046068469760")]
    [InlineData("""units.parse("b")""", "0", true)]
    [InlineData("""units.parse(`1.2"ki"`)""", "1228.8")]
    [InlineData("""units.parse(`-1b`)""", "0", true)]
    public async Task UnitsParse(string func, string expected, bool fails = false)
    {
        var result = await RunTestCase(func, expected, fails);
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ErrorHandling(bool strictErrors)
    {
        var src = """
            package sdk

            import rego.v1

            allow if {
                io.jwt.verify_hs256("xxxxx", "secret")
                [_, payload, _] := io.jwt.decode("xxxxx")
                payload.role == "admin"
            }

            reason contains "invalid JWT supplied as input" if {
                not io.jwt.decode("xxxxx")
            }
            """;

        using var eval = await Build(src, "sdk", new DefaultOpaImportsAbi(), new() { StrictBuiltinErrors = strictErrors });

        if (strictErrors)
            Assert.Throws<OpaEvaluationException>(() => eval.EvaluateRaw(null, "sdk"));
        else
        {
            var result = eval.EvaluateRaw(null, "sdk");
            var expected = """[{"result":{"reason":["invalid JWT supplied as input"]}}]""";
            Assert.Equal(expected, result);
        }
    }

    [Theory]
    [InlineData("""numbers.range_step(1, 3, 1)""", "[1,2,3]")]
    [InlineData("""numbers.range_step(3, 1, 1)""", "[3,2,1]")]
    [InlineData("""numbers.range_step(1, 6, 2)""", "[1,3,5]")]
    [InlineData("""numbers.range_step(1, 1, 2)""", "[1]")]
    [InlineData("""numbers.range_step(1, 1, -2)""", "[]", true)]
    public async Task NumbersRangeStep(string func, string expected, bool fails = false)
    {
        var result = await RunTestCase(func, expected, fails);
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""glob.quote_meta(`[foo*]`)""", @"`\[foo\*\]`")]
    [InlineData("""glob.quote_meta(`{foo*}`)""", @"`\{foo\*\}`")]
    [InlineData("""glob.quote_meta(`*?\[]{}`)""", @"`\*\?\\\[\]\{\}`")]
    [InlineData("""glob.quote_meta(`some text and *?\[]{}`)""", @"`some text and \*\?\\\[\]\{\}`")]
    public async Task GlobQuoteMeta(string func, string expected, bool fails = false)
    {
        var result = await RunTestCase(func, expected, fails);
        Assert.True(result.Assert);
    }

    private const string GraphSimple = """
        {
            "root": [ "lvl1" ],
            "lvl1": [ "lvl2", "lvl3" ],
            "lvl2": [ "lvl4" ],
            "lvl3": [],
            "lvl4": [],
        }
        """;

    private const string GraphNoEdge = """
        {
            "root": [ "lvl1" ],
            "lvl1": [ "lvl2", "lvl3" ],
            "lvl2": [ "lvl4", "na" ],
            "lvl3": [],
            "lvl4": [],
        }
        """;

    private const string GraphMixed = """
        {
            "root": { "lvl1" },
            "lvl1": { "lvl2", "lvl3" },
            "lvl2": [ "lvl4" ],
            "lvl3": [],
            "lvl4": [],
        }
        """;

    [Theory]
    [InlineData(
        $"graph.reachable_paths({GraphSimple}, {{ \"root\" }})",
        """{ [ "root", "lvl1", "lvl2", "lvl4" ], [ "root", "lvl1", "lvl3" ] }"""
        )]
    [InlineData(
        $"graph.reachable_paths({GraphMixed}, {{ \"root\" }})",
        """{ [ "root", "lvl1", "lvl2", "lvl4" ], [ "root", "lvl1", "lvl3" ] }"""
        )]
    [InlineData(
        $"graph.reachable_paths({GraphNoEdge}, {{ \"root\" }})",
        """{ [ "root", "lvl1", "lvl2", "lvl4" ], [ "root", "lvl1", "lvl3" ], [ "root", "lvl1", "lvl2" ] }"""
        )]
    public async Task GraphReachablePaths(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }
}