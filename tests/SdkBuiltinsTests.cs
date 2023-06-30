using System.Text.Json.Nodes;

using Microsoft.Extensions.Options;

using OpaDotNet.Tests.Common;
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

using Wasmtime;

using Xunit.Abstractions;

namespace OpaDotNet.Tests;

public class SdkBuiltinsTests
{
    private readonly ILoggerFactory _loggerFactory;

    private readonly OptionsWrapper<RegoCliCompilerOptions> _options = new(new());

    public SdkBuiltinsTests(ITestOutputHelper output)
    {
        _loggerFactory = new LoggerFactory(new[] { new XunitLoggerProvider(output) });
    }
    
    [Fact]
    public async Task StringsIndexOfN()
    {
        var src = """
package sdk
t1 := o { o := indexof_n("sad", "a") }
t2 := o { o := indexof_n("sadad", "a") }
t3 := o { o := indexof_n("sad", "x") }
""";
        
        using var eval = await Build(src, "sdk");
        var result = eval.EvaluateValue(new { t1 = Array.Empty<int>(), t2 = Array.Empty<int>(), t3 = Array.Empty<int>() }, "sdk");
        Assert.Collection(result.t1, p => Assert.Equal(1, p));
        Assert.Collection(result.t2, p => Assert.Equal(1, p), p => Assert.Equal(3, p));
        Assert.Empty(result.t3);
    }
    
//     [Fact]
//     public async Task StringsSprintf()
//     {
//         var src = """
// package sdk
// t1 := o { o := sprintf("sad", "a") }
// """;
//         
//         using var eval = await Build(src, "sdk");
//         var result = eval.EvaluateValue(new { t1 = string.Empty }, "sdk");
//         
//     }
    
    [Fact]
    public async Task StringsAnyPrefixMatch()
    {
        var src = """
package sdk
t1 := o { o := strings.any_prefix_match(["aaa", "bbb", "ccc"], ["bb"]) }
t2 := o { o := strings.any_prefix_match(["aaa", "bbb", "ccc"], ["xx", "yy", "cc"]) }
t3 := o { o := strings.any_prefix_match(["aaa", "bbb", "ccc"], ["xx"]) }
""";
        
        using var eval = await Build(src, "sdk");
        var result = eval.EvaluateValue(new { t1 = false, t2 = false, t3 = false }, "sdk");
        
        Assert.True(result.t1);
        Assert.True(result.t2);
        Assert.False(result.t3);
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
        public DateTimeOffset GetNow => Now();
        
        protected override DateTimeOffset Now()
        {
            return new DateTimeOffset(2023, 6, 5, 14, 27, 39, TimeSpan.Zero);
        }
    }
    
    [Fact]
    public async Task Time()
    {
        var src = """
package sdk
t1 := o { o := time.add_date(1672575347000000000, 1, 2, 3) }
t2 := o { o := time.clock(1709554547000000000) }
t3 := o { o := time.date(1709554547000000000) }
t4 := o { o := time.now_ns() }
t5 := o { o := time.diff(1687527385064073200, 1672575347000000000) }
t6 := o { o := time.weekday(1687527385064073200) }
""";
        var imports = new TimeImports();
        using var eval = await Build(src, "sdk", imports);
        
        var result = eval.EvaluateValue(
            new
            {
                t1 = 0L, 
                t2 = Array.Empty<int>(), 
                t3 = Array.Empty<int>(),
                t4 = 0L,
                t5 = Array.Empty<int>(),
                t6 = string.Empty,
            }, 
            "sdk"
            );
        
        Assert.Equal(1709554547000000000, result.t1);
        Assert.Collection(result.t2, p => Assert.Equal(12, p), p => Assert.Equal(15, p), p => Assert.Equal(47, p));
        Assert.Collection(result.t3, p => Assert.Equal(2024, p), p => Assert.Equal(3, p), p => Assert.Equal(4, p));
        Assert.Collection(result.t3, p => Assert.Equal(2024, p), p => Assert.Equal(3, p), p => Assert.Equal(4, p));
        
        var nowNs = (imports.GetNow.Ticks - DateTimeOffset.UnixEpoch.Ticks) * 100;
        Assert.Equal(nowNs, result.t4);
        
        Assert.Collection(
            result.t5, 
            p => Assert.Equal(0, p),
            p => Assert.Equal(5, p),
            p => Assert.Equal(22, p),
            p => Assert.Equal(1, p),
            p => Assert.Equal(20, p),
            p => Assert.Equal(38, p)
            );
        
        Assert.Equal("Friday", result.t6);
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
    
    [Fact]
    public async Task NetCidrContains()
    {
        var src = """
package sdk
t1 := o { o := net.cidr_contains("127.0.0.64/24", "127.0.0.64/26") }
t2 := o { o := net.cidr_contains("127.0.0.64/24", "127.0.0.1") }
t3 := o { o := net.cidr_contains("127.0.0.64/26", "127.0.0.64/24") }
t4 := o { o := net.cidr_contains("127.0.0.64/24", "127.10.0.1") }
""";
        using var eval = await Build(src, "sdk");
        
        var result = eval.EvaluateValue(
            new { t1 = false, t2 = false, t3 = true, t4 = true },
            "sdk"
            );
        
        Assert.True(result.t1);
        Assert.True(result.t2);
        Assert.False(result.t3);
        Assert.False(result.t4);
    }
    
    [Fact]
    public async Task NetCidrContainsMatches()
    {
        var src = """
package sdk
t1 := o { o := net.cidr_contains_matches(["127.0.0.64/24", "10.0.0.64/24"], ["127.0.0.64/26", "127.0.0.1", "10.0.0.100", "1.0.0.1"]) }
t2 := o { o := net.cidr_contains_matches("1.1.1.0/24", "1.1.1.128") }
t3 := o { o := net.cidr_contains_matches(["1.1.1.0/24", "1.1.2.0/24"], "1.1.1.128") }
t4 := o { o := net.cidr_contains_matches(["1.1.1.0/24", "1.1.2.0/24", "foo"], "1.1.1.128") }
""";
        using var eval = await Build(src, "sdk");
        
        var result = eval.EvaluateValue(
            new
            {
                t1 = Array.Empty<int[]>(), 
                t2 = Array.Empty<string[]>(),
                t3 = Array.Empty<JsonArray?>(),
                t4 = Array.Empty<JsonArray?>(),
            },
            "sdk"
            );
        
        Assert.Collection(
            result.t1,
            p => Assert.Collection(p, pp => Assert.Equal(0, pp), pp => Assert.Equal(0, pp)),
            p => Assert.Collection(p, pp => Assert.Equal(0, pp), pp => Assert.Equal(1, pp)),
            p => Assert.Collection(p, pp => Assert.Equal(1, pp), pp => Assert.Equal(2, pp))
            );
        
        Assert.Collection(
            result.t2,
            p => Assert.Collection(p, pp => Assert.Equal("1.1.1.0/24", pp), pp => Assert.Equal("1.1.1.128", pp))
            );
        
        Assert.NotNull(result.t3);
        Assert.Collection(
            result.t3,
            p => Assert.Collection(p, pp => Assert.Equal(0, pp.GetValue<int>()), pp => Assert.Equal("1.1.1.128", pp.GetValue<string>()))
            );
        
        Assert.Null(result.t4);
    }

    private const string NetCidrContainsMatchesArrayObjectsCase1 = """
t1 := o { o := net.cidr_contains_matches([["1.1.0.0/16", "foo", 1], "1.1.2.0/24"], {"x": "1.1.1.128", "y": ["1.1.254.254", "bar"]}) }
""";
    
    private const string NetCidrContainsMatchesArrayObjectsCase2 = """
t1 := o { o := net.cidr_contains_matches([["1.1.2.0/24", "foo", 1], "1.1.0.0/16"], {"x": "1.1.1.128", "y": ["1.1.254.254", "bar"]}) }
""";
    
    [Theory]
    [InlineData(NetCidrContainsMatchesArrayObjectsCase1, 0)]
    [InlineData(NetCidrContainsMatchesArrayObjectsCase2, 1)]
    public async Task NetCidrContainsMatchesArrayObjects(string s, int expectedIndex)
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
                pp => Assert.Equal(expectedIndex, pp.GetValue<int>()), 
                pp => Assert.Equal("y", pp.GetValue<string>())
                ),
            p => Assert.Collection(
                p, 
                pp => Assert.Equal(expectedIndex, pp.GetValue<int>()), 
                pp => Assert.Equal("x", pp.GetValue<string>())
                ) 
            );
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
                pp => Assert.Equal(expectedIndex, pp.GetValue<int>()), 
                pp => Assert.Equal("y", pp.GetValue<string>())
                ),
            p => Assert.Collection(
                p, 
                pp => Assert.Equal(expectedIndex, pp.GetValue<int>()), 
                pp => Assert.Equal("x", pp.GetValue<string>())
                ) 
            );
    }

    [Fact]
    public async Task Net()
    {
        var src = """
package sdk
t1 := o { o := net.cidr_contains_matches(["127.0.0.64/24", "10.0.0.64/24"], ["127.0.0.64/26", "127.0.0.1", "10.0.0.100", "1.0.0.1"]) }
t2 := o { o := net.cidr_expand("192.168.0.0/30") }
t31 := o { o := net.cidr_is_valid("192.168.0.0/30") }
t32 := o { o := net.cidr_is_valid("192.168.0.500/30") }
""";
        using var eval = await Build(src, "sdk");
        
        var result = eval.EvaluateValue(
            new
            {
                t1 = Array.Empty<int[]>(), 
                t2 = Array.Empty<string>(), 
                t31 = false, 
                t32 = true,
                t41 = Array.Empty<string>(),
                t42 = Array.Empty<string>(),
            },
            "sdk"
            );
        
        Assert.Collection(
            result.t1,
            p => Assert.Collection(p, pp => Assert.Equal(0, pp), pp => Assert.Equal(0, pp)),
            p => Assert.Collection(p, pp => Assert.Equal(0, pp), pp => Assert.Equal(1, pp)),
            p => Assert.Collection(p, pp => Assert.Equal(1, pp), pp => Assert.Equal(2, pp))
            );
        
        Assert.Collection(
            result.t2,
            p => Assert.Equal("192.168.0.0", p),
            p => Assert.Equal("192.168.0.1", p),
            p => Assert.Equal("192.168.0.2", p),
            p => Assert.Equal("192.168.0.3", p)
            );
        
        Assert.True(result.t31);
        Assert.False(result.t32);
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
        return factory.CreateFromBundle(policy);
    }
}