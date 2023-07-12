using System.Text;
using System.Text.Json.Nodes;

using JetBrains.Annotations;

using Microsoft.Extensions.Options;

using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

using Xunit.Abstractions;

// ReSharper disable StringLiteralTypo

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

    private class TimeImports : TestImportsAbi
    {
        public TimeImports(ITestOutputHelper output) : base(output)
        {
        }

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
        var result = await RunTestCase(func, expected, new TimeImports(_output));
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""time.parse_duration_ns("-10s")""", "-10000000000")]
    [InlineData("""time.parse_duration_ns("1µs")""", "1000")]
    [InlineData("""time.parse_duration_ns("1.5h")""", "5400000000000")]
    [InlineData("""time.parse_duration_ns("1ns")""", "1")]
    public async Task TimeParseDurationNs(string func, string expected)
    {
        var result = await RunTestCase(func, expected, new TimeImports(_output));
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""time.parse_rfc3339_ns("1985-04-12T23:20:50.52Z")""", "482196050520000000")]
    [InlineData("""time.parse_rfc3339_ns("1996-12-19T16:39:57-08:00")""", "851042397000000000")]
    [InlineData("""time.parse_rfc3339_ns("1990-12-31T23:59:59Z")""", "662687999000000000")]
    [InlineData("""time.parse_rfc3339_ns("1937-01-01T12:00:27.87+00:20")""", "-1041337172130000000")]
    public async Task TimeParseRfc3339Ns(string func, string expected)
    {
        var result = await RunTestCase(func, expected, new TimeImports(_output));
        Assert.True(result.Assert);
    }

    private class DebugImports : DefaultOpaImportsAbi
    {
        public StringBuilder Output { get; } = new();

        public override void PrintLn(string message)
        {
            throw new Exception("Boom!");
        }

        protected override bool Trace(string message)
        {
            Output.Append(message);
            return base.Trace(message);
        }
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
        using var eval = await Build(src, "sdk", import);

        var result = eval.EvaluateValue(new { t1 = string.Empty }, "sdk");

        Assert.Equal("hi!", result.t1);
        Assert.Equal("hi!", import.Output.ToString());
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
        var result = await RunTestCase(func, expected, new TimeImports(_output));
        Assert.True(result.Assert);
    }

    [Theory]
    [InlineData("""net.cidr_merge(["192.0.128.0/24", "192.0.129.0/24"])""", """{"192.0.128.0/23"}""")]
    [InlineData("net.cidr_is_valid(\"192.168.0.0/30\")", "true")]
    [InlineData("net.cidr_is_valid(\"192.168.0.500/30\")", "false")]
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

    [Theory]
    [InlineData("""crypto.md5("message")""", "\"78e731027d8fd50ed642340b7c9a63b3\"")]
    [InlineData("""crypto.sha1("message")""", "\"6f9b9af3cd6e8b8a73c2cdced37fe9f59226e27d\"")]
    [InlineData("""crypto.sha256("message")""", "\"ab530a13e45914982b79f9b7e3fba994cfd1f3fb22f71cea1afbf02b460c6d1d\"")]
    [InlineData("""crypto.hmac.equal("4e4748e62b463521f6775fbf921234b5", "4e4748e62b463521f6775fbf921234b5")""", "true")]
    [InlineData("""crypto.hmac.equal("4e4748e62b463521f6775fbf921234bx", "4e4748e62b463521f6775fbf921234b5")""", "false")]
    [InlineData("""crypto.hmac.md5("message", "key")""", "\"4e4748e62b463521f6775fbf921234b5\"")]
    [InlineData("""crypto.hmac.sha1("message", "key")""", "\"2088df74d5f2146b48146caf4965377e9d0be3a4\"")]
    [InlineData("""crypto.hmac.sha256("message", "key")""", "\"6e9ef29b75fffc5b7abae527d58fdadb2fe42e7219011976917343065f58ed4a\"")]
    [InlineData("""crypto.hmac.sha512("message", "key")""", "\"e477384d7ca229dd1426e64b63ebf2d36ebd6d7e669a6735424e72ea6c01d3f8b56eb39c36d8232f5427999b8d1a3f9cd1128fc69f4d75b434216810fa367e98\"")]
    public async Task Crypto(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
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
    [InlineData(
        """io.jwt.decode("eyJ0eXAiOiAiSldUIiwgImFsZyI6ICJFUzI1NiJ9.eyJuYmYiOiAxNDQ0NDc4NDAwLCAiaXNzIjogInh4eCJ9.lArczfN-pIL8oUU-7PU83u-zfXougXBZj6drFeKFsPEoVhy9WAyiZlRshYqjTSXdaw8yw2L-ovt4zTUZb2PWMg")""",
        """[{"typ": "JWT", "alg": "ES256"}, {"nbf": 1444478400, "iss": "xxx"}, "940adccdf37ea482fca1453eecf53cdeefb37d7a2e8170598fa76b15e285b0f128561cbd580ca266546c858aa34d25dd6b0f32c362fea2fb78cd35196f63d632"]"""
        )]
    public async Task Jwt(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    private const string JwtVerifyIssToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJ4eHgifQ.Mt8_pnXt43Dh1SnoOQLSzXHnb3BPoTa4ATIDXJig0g8";
    private const string JwtVerifyIssSecret = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Theory]
    [InlineData(
        $$"""io.jwt.verify_hs256("{{JwtVerifyIssToken}}", "{{JwtVerifyIssSecret}}")""",
        """true"""
        )]
    [InlineData(
        $$"""io.jwt.decode_verify("{{JwtVerifyIssToken}}", {"iss": "xxx", "secret": "{{JwtVerifyIssSecret}}"})""",
        """[true,{"alg":"HS256","typ":"JWT"},{"iss":"xxx"}]"""
        )]
    [InlineData(
        $$"""io.jwt.decode_verify("{{JwtVerifyIssToken}}", {"iss": "yyy", "secret": "{{JwtVerifyIssSecret}}"})""",
        """[false,{},{}]"""
        )]
    public async Task JwtVerifyIss(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    private const string JwtVerifyAudToken = "eyJhbGciOiJIUzM4NCIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJ4eHgiLCJhdWQiOiJhYWEifQ.xZbdtbn-es4obumv6H1DVrdiZL8GTOya-ujROx63yots_FjvG_5fop00c0ah6MNB";
    private const string JwtVerifyAudSecret = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Theory]
    [InlineData(
        $$"""io.jwt.verify_hs384("{{JwtVerifyAudToken}}", "{{JwtVerifyAudSecret}}")""",
        """true"""
        )]
    [InlineData(
        $$"""io.jwt.decode_verify("{{JwtVerifyAudToken}}", {"aud": "aaa", "secret": "{{JwtVerifyAudSecret}}"})""",
        """[true,{"alg":"HS384","typ":"JWT"},{"iss": "xxx", "aud":"aaa"}]"""
        )]
    [InlineData(
        $$"""io.jwt.decode_verify("{{JwtVerifyAudToken}}", {"aud": "bbb", "secret": "{{JwtVerifyAudSecret}}"})""",
        """[false,{},{}]"""
        )]
    [InlineData(
        $$"""io.jwt.decode_verify("{{JwtVerifyAudToken}}", {"secret": "{{JwtVerifyAudSecret}}"})""",
        """[false,{},{}]"""
        )]
    public async Task JwtVerifyAud(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    private const string JwtVerifyTimeToken = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJ4eHgiLCJleHAiOjE2ODkxNDE4NDcsIm5iZiI6MTY4ODk2OTA0N30.iaQZYESw0-enygzry1EYKT7_xiNGhqExlWG62fUmt3mhb31LXNLKEZL_ki-nhDuQf6hkydakXpIc6m6lVIp-iQ";
    private const string JwtVerifyTimeSecret = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Theory]
    [InlineData(
        $$"""io.jwt.verify_hs512("{{JwtVerifyTimeToken}}", "{{JwtVerifyTimeSecret}}")""",
        """true"""
        )]
    [InlineData(
        $$"""io.jwt.decode_verify("{{JwtVerifyTimeToken}}", {"time": 1689055447000000000, "secret": "{{JwtVerifyTimeSecret}}"})""",
        """[true,{"alg":"HS512","typ":"JWT"},{"iss": "xxx", "exp": 1689141847, "nbf": 1688969047}]"""
        )]
    public async Task JwtVerifyTime(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
        Assert.True(result.Assert);
    }

    [Fact]
    public async Task JwtJwkCerts()
    {
        var src = """
package sdk

es256_token := "eyJ0eXAiOiAiSldUIiwgImFsZyI6ICJFUzI1NiJ9.eyJuYmYiOiAxNDQ0NDc4NDAwLCAiaXNzIjogInh4eCJ9.lArczfN-pIL8oUU-7PU83u-zfXougXBZj6drFeKFsPEoVhy9WAyiZlRshYqjTSXdaw8yw2L-ovt4zTUZb2PWMg"

jwks := `{
    "keys": [{
        "kty":"EC",
        "crv":"P-256",
        "x":"z8J91ghFy5o6f2xZ4g8LsLH7u2wEpT2ntj8loahnlsE",
        "y":"7bdeXLH61KrGWRdh7ilnbcGQACxykaPKfmBccTHIOUo"
    }]
}`

r := io.jwt.verify_es256(es256_token, jwks)
""";
        using var eval = await Build(src, "sdk");

        var result = eval.EvaluateValue(
            new { r = false, },
            "sdk"
            );

        Assert.True(result.r);
    }

    [Fact]
    public async Task JwtPemCerts()
    {
        var src = """
package sdk

es256_token := "eyJ0eXAiOiAiSldUIiwgImFsZyI6ICJFUzI1NiJ9.eyJuYmYiOiAxNDQ0NDc4NDAwLCAiaXNzIjogInh4eCJ9.lArczfN-pIL8oUU-7PU83u-zfXougXBZj6drFeKFsPEoVhy9WAyiZlRshYqjTSXdaw8yw2L-ovt4zTUZb2PWMg"

cert := `-----BEGIN CERTIFICATE-----
MIIBcDCCARagAwIBAgIJAMZmuGSIfvgzMAoGCCqGSM49BAMCMBMxETAPBgNVBAMM
CHdoYXRldmVyMB4XDTE4MDgxMDE0Mjg1NFoXDTE4MDkwOTE0Mjg1NFowEzERMA8G
A1UEAwwId2hhdGV2ZXIwWTATBgcqhkjOPQIBBggqhkjOPQMBBwNCAATPwn3WCEXL
mjp/bFniDwuwsfu7bASlPae2PyWhqGeWwe23Xlyx+tSqxlkXYe4pZ23BkAAscpGj
yn5gXHExyDlKo1MwUTAdBgNVHQ4EFgQUElRjSoVgKjUqY5AXz2o74cLzzS8wHwYD
VR0jBBgwFoAUElRjSoVgKjUqY5AXz2o74cLzzS8wDwYDVR0TAQH/BAUwAwEB/zAK
BggqhkjOPQQDAgNIADBFAiEA4yQ/88ZrUX68c6kOe9G11u8NUaUzd8pLOtkKhniN
OHoCIHmNX37JOqTcTzGn2u9+c8NlnvZ0uDvsd1BmKPaUmjmm
-----END CERTIFICATE-----`

r := io.jwt.verify_es256(es256_token, cert)
""";
        using var eval = await Build(src, "sdk");

        var result = eval.EvaluateValue(
            new { r = false, },
            "sdk"
            );

        Assert.True(result.r);
    }

    [Theory]
    [InlineData(
        """
io.jwt.encode_sign({
    "typ": "JWT",
    "alg": "HS256"
}, {
    "iss": "joe",
    "exp": 1300819380,
    "aud": ["bob", "saul"],
    "http://example.com/is_root": true,
    "privateParams": {
        "private_one": "one",
        "private_two": "two"
    }
}, {
    "kty": "oct",
    "k": "AyM1SysPpbyDfgZld3umj1qzKObwVMkoqQ-EstJQLr_T-1qS0gZH75aKtMN3Yj0iPS4hcgUuTwjAzZr1Z9CAow"
})
""",
        "\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vZXhhbXBsZS5jb20vaXNfcm9vdCI6dHJ1ZSwiaXNzIjoiam9lIiwicHJpdmF0ZVBhcmFtcyI6eyJwcml2YXRlX3R3byI6InR3byIsInByaXZhdGVfb25lIjoib25lIn0sImF1ZCI6WyJib2IiLCJzYXVsIl0sImV4cCI6MTMwMDgxOTM4MH0.hBeTHKH5VfWc1y502RQytpQQg5UzvLyxWqQVa2mmRAU\""
        )]
    [InlineData(
        """
io.jwt.encode_sign({
    "typ": "JWT",
    "alg": "HS256"},
    {}, {
    "kty": "oct",
    "k": "AyM1SysPpbyDfgZld3umj1qzKObwVMkoqQ-EstJQLr_T-1qS0gZH75aKtMN3Yj0iPS4hcgUuTwjAzZr1Z9CAow"
})
""",
        "\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.e30.6cvao8lnOu6FAdK68jQFcDMXOmaWNwWiYhCgijd-AD8\""
        )]
    [InlineData(
        """
io.jwt.encode_sign({
    "alg": "RS256"
}, {
    "iss": "joe",
    "exp": 1300819380,
    "aud": ["bob", "saul"],
    "http://example.com/is_root": true,
    "privateParams": {
        "private_one": "one",
        "private_two": "two"
    }
},
{
    "kty": "RSA",
    "n": "ofgWCuLjybRlzo0tZWJjNiuSfb4p4fAkd_wWJcyQoTbji9k0l8W26mPddxHmfHQp-Vaw-4qPCJrcS2mJPMEzP1Pt0Bm4d4QlL-yRT-SFd2lZS-pCgNMsD1W_YpRPEwOWvG6b32690r2jZ47soMZo9wGzjb_7OMg0LOL-bSf63kpaSHSXndS5z5rexMdbBYUsLA9e-KXBdQOS-UTo7WTBEMa2R2CapHg665xsmtdVMTBQY4uDZlxvb3qCo5ZwKh9kG4LT6_I5IhlJH7aGhyxXFvUK-DWNmoudF8NAco9_h9iaGNj8q2ethFkMLs91kzk2PAcDTW9gb54h4FRWyuXpoQ",
    "e": "AQAB",
    "d": "Eq5xpGnNCivDflJsRQBXHx1hdR1k6Ulwe2JZD50LpXyWPEAeP88vLNO97IjlA7_GQ5sLKMgvfTeXZx9SE-7YwVol2NXOoAJe46sui395IW_GO-pWJ1O0BkTGoVEn2bKVRUCgu-GjBVaYLU6f3l9kJfFNS3E0QbVdxzubSu3Mkqzjkn439X0M_V51gfpRLI9JYanrC4D4qAdGcopV_0ZHHzQlBjudU2QvXt4ehNYTCBr6XCLQUShb1juUO1ZdiYoFaFQT5Tw8bGUl_x_jTj3ccPDVZFD9pIuhLhBOneufuBiB4cS98l2SR_RQyGWSeWjnczT0QU91p1DhOVRuOopznQ",
    "p": "4BzEEOtIpmVdVEZNCqS7baC4crd0pqnRH_5IB3jw3bcxGn6QLvnEtfdUdiYrqBdss1l58BQ3KhooKeQTa9AB0Hw_Py5PJdTJNPY8cQn7ouZ2KKDcmnPGBY5t7yLc1QlQ5xHdwW1VhvKn-nXqhJTBgIPgtldC-KDV5z-y2XDwGUc",
    "q": "uQPEfgmVtjL0Uyyx88GZFF1fOunH3-7cepKmtH4pxhtCoHqpWmT8YAmZxaewHgHAjLYsp1ZSe7zFYHj7C6ul7TjeLQeZD_YwD66t62wDmpe_HlB-TnBA-njbglfIsRLtXlnDzQkv5dTltRJ11BKBBypeeF6689rjcJIDEz9RWdc",
    "dp": "BwKfV3Akq5_MFZDFZCnW-wzl-CCo83WoZvnLQwCTeDv8uzluRSnm71I3QCLdhrqE2e9YkxvuxdBfpT_PI7Yz-FOKnu1R6HsJeDCjn12Sk3vmAktV2zb34MCdy7cpdTh_YVr7tss2u6vneTwrA86rZtu5Mbr1C1XsmvkxHQAdYo0",
    "dq": "h_96-mK1R_7glhsum81dZxjTnYynPbZpHziZjeeHcXYsXaaMwkOlODsWa7I9xXDoRwbKgB719rrmI2oKr6N3Do9U0ajaHF-NKJnwgjMd2w9cjz3_-kyNlxAr2v4IKhGNpmM5iIgOS1VZnOZ68m6_pbLBSp3nssTdlqvd0tIiTHU",
    "qi": "IYd7DHOhrWvxkwPQsRM2tOgrjbcrfvtQJipd-DlcxyVuuM9sQLdgjVk2oy26F0EmpScGLq2MowX7fhd_QJQ3ydy5cY7YIBi87w93IKLEdfnbJtoOPLUW0ITrJReOgo1cq9SbsxYawBgfp_gh6A5603k2-ZQwVK0JKSHuLFkuQ3U"
})
""",
        "\"eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vZXhhbXBsZS5jb20vaXNfcm9vdCI6dHJ1ZSwiaXNzIjoiam9lIiwicHJpdmF0ZVBhcmFtcyI6eyJwcml2YXRlX3R3byI6InR3byIsInByaXZhdGVfb25lIjoib25lIn0sImF1ZCI6WyJib2IiLCJzYXVsIl0sImV4cCI6MTMwMDgxOTM4MH0.UkC7cqfGzNIOLkUguoqZQOS3LBakt04RJiAylHz6iv5_MtVnqSuhF0agWEPQTjX-5rf3T_Gz-7BaNrw18Fv7wb07FO97pbrdLfcEUTC65qhNjk9lTpyt-m_ICdEF03XDcORC1nnuzKdKk25FUvUtotD5cnZ7o7xgv-HwOU7srEhoudlezB6GulcwMiRyIh17LyjNdCCMsOhrHMFPEMbCWO4IL1OL8Ohns2kxcPDoGnYiiWbAkQCWAXAp8DWCcUaqFjKOY3-GFHFBEBfyRwO_-c8hxx4i1glaPXR2i8OXRuhs7k25s5V0qZNcFjCLeVpmDXrJe3NcetntAoCmPoAR7A\""
        )]
    [InlineData(
        """
io.jwt.encode_sign_raw(
    `{"typ":"JWT","alg":"HS256"}`,
     `{"iss":"joe","exp":1300819380,"http://example.com/is_root":true}`,
    `{"kty":"oct","k":"AyM1SysPpbyDfgZld3umj1qzKObwVMkoqQ-EstJQLr_T-1qS0gZH75aKtMN3Yj0iPS4hcgUuTwjAzZr1Z9CAow"}`
)
""",
        "\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJqb2UiLCJleHAiOjEzMDA4MTkzODAsImh0dHA6Ly9leGFtcGxlLmNvbS9pc19yb290Ijp0cnVlfQ.d6nMDXnJZfNNj-1o1e75s6d0six0lkLp5hSrGaz4o9A\""
        )]
    public async Task JwtEncodeSign(string func, string expected)
    {
        var result = await RunTestCase(func, expected);
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
        _output.WriteLine(result.r.ToString());
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private record TestCaseResult
    {
        public bool Assert { get; [UsedImplicitly] set; }
        public JsonNode Expected { get; [UsedImplicitly] set; } = default!;
        public JsonNode? Actual { get; [UsedImplicitly] set; }
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
        var factory = new OpaEvaluatorFactory(imports ?? new TestImportsAbi(_output));

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