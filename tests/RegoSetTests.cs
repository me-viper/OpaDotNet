using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Rego;

namespace OpaDotNet.Tests;

public class RegoSetTests
{
    public static IEnumerable<object[]> ParserTestCases()
    {
        yield return new object[]
        {
            """{"__rego_set":["1","2",{"__rego_set":[3,4]}]}""",
            """{"1","2",{3,4}}""",
        };
        yield return new object[]
        {
            """{"__rego_set":[]}""",
            """{}""",
        };
        yield return new object[]
        {
            """{"__rego_set":[1, {"a":"b"}]}""",
            """{1, {"a":"b"}}""",
        };
        yield return new object[]
        {
            """{"__rego_set":[1, [2, 3]]}""",
            """{1, [2, 3]}""",
        };
        yield return new object[]
        {
            """{"__rego_set":[1, {"a":{"__rego_set":["y","z"]}}]}""",
            """{1, {"a":{"y","z"}}}""",
        };
    }

    [Theory]
    [MemberData(nameof(ParserTestCases))]
    public void Parser(string input, string expected)
    {
        var result = RegoValueHelper.SetFromJson(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SerializeSetOfAny()
    {
        var o = new object[] { "1", "2", new RegoSetOfAny(new object[] { 3, 4 }) };

        var opts = new JsonSerializerOptions
        {
            Converters = { RegoSetJsonConverterFactory.Instance },
        };

        var s = JsonSerializer.Serialize(new RegoSetOfAny(o), opts);
        Assert.Equal("""{"__rego_set":["1","2",{"__rego_set":[3,4]}]}""", s);
    }

    [Fact]
    public void SerializeSetOfStrings()
    {
        var o = new RegoSet<string>(new[] { "1", "2" });

        var opts = new JsonSerializerOptions
        {
            Converters = { RegoSetJsonConverterFactory.Instance },
        };

        var s = JsonSerializer.Serialize(o, opts);
        Assert.Equal("""{"__rego_set":["1","2"]}""", s);
    }

    [Fact]
    public void Deserialize()
    {
        // var s = "{\"1\",\"2\",{3,4}}";
        // var opts = new JsonSerializerOptions();

        // var result = JsonSerializer.Deserialize<RegoAnySet>(RegoAnySet.StringToJsonString(s), opts);
        //
        // Assert.NotNull(result);
        // Assert.Collection(
        //     result.Set,
        //     p => Assert.Equal("1", p),
        //     p => Assert.Equal("2", p),
        //     p => Assert.Collection(((RegoAnySet)p).Set, pp => Assert.Equal(3, pp), pp => Assert.Equal(4, pp))
        //     );
    }
}