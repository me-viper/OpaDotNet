using System.Text.Json.Nodes;

using OpaDotNet.Wasm.Rego;

namespace OpaDotNet.Wasm.Tests;

public class RegoSetTests
{
    public static IEnumerable<object[]> RegoValueTestCases()
    {
        yield return new object[]
        {
            """[{"__rego_set":["1","2",[{"__rego_set":[3,4]}]]}]""",
            """{"1","2",{3,4}}""",
        };
        yield return new object[]
        {
            """[{"__rego_set":[]}]""",
            """{}""",
        };
        yield return new object[]
        {
            """[{"__rego_set":[1, {"a":"b"}]}]""",
            """{1, {"a":"b"}}""",
        };
        yield return new object[]
        {
            """[{"__rego_set":[1, [2, 3]]}]""",
            """{1, [2, 3]}""",
        };
        yield return new object[]
        {
            """[{"__rego_set":[1, {"a":[{"__rego_set":["y","z"]}]}]}]""",
            """{1, {"a":{"y","z"}}}""",
        };
        yield return new object[]
        {
            """[1, 2]""",
            """[1, 2]""",
        };

        // yield return new object[]
        // {
        //     """[{"__rego_set":[1, {"a":{"__rego_set":["y","z"]}}]}]""",
        //     """{1, {"a":{"y","z"}}}""",
        // };
    }

    [Theory]
    [MemberData(nameof(RegoValueTestCases))]
    public void ToRegoValue(string input, string expected)
    {
        var result = RegoValueHelper.JsonToRegoValue(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(RegoValueTestCases))]
    public void FromRegoValue(string expected, string input)
    {
        var result = RegoValueHelper.JsonFromRegoValue(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SerializeSetOfAny()
    {
        var o = new object[] { "1", "2", new RegoSet<object>(new object[] { 3, 4 }) };

        var opts = new JsonSerializerOptions
        {
            Converters = { RegoSetJsonConverterFactory.Instance },
        };

        var s = JsonSerializer.Serialize(new RegoSet<object>(o), opts);
        Assert.Equal("""[{"__rego_set":["1","2",[{"__rego_set":[3,4]}]]}]""", s);
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
        Assert.Equal("""[{"__rego_set":["1","2"]}]""", s);
    }

    [Fact]
    public void Deserialize()
    {
        var s = "{\"1\",\"2\",{3,4}}";

        var opts = new JsonSerializerOptions
        {
            Converters = { RegoSetJsonConverterFactory.Instance },
        };

        var result = JsonSerializer.Deserialize<RegoSet<JsonNode>>(RegoValueHelper.JsonFromRegoValue(s), opts);

        Assert.NotNull(result);
        Assert.Collection(
            result.Set,
            p => Assert.Equal("1", p.AsValue().GetValue<string>()),
            p => Assert.Equal("2", p.AsValue().GetValue<string>()),
            p =>
            {
                Assert.True(p.IsRegoSet());

                p.TryGetRegoSet<int>(out var set, opts);

                Assert.NotNull(set);
                Assert.Collection(
                    set.Set,
                    pp => Assert.Equal(3, pp),
                    pp => Assert.Equal(4, pp)
                    );
            }
            );
    }

    [Fact]
    public void DeserializeAsJsonNodes()
    {
        var s = "{1, 2, 3}";
        var json = RegoValueHelper.JsonFromRegoValue(s);
        var jsonArray = JsonNode.Parse(json)?.AsArray();

        Assert.NotNull(jsonArray);

        var result = jsonArray.TryGetRegoSet(out var set);

        Assert.True(result);
        Assert.NotNull(set);

        Assert.Collection(
            set.Set,
            p => Assert.Equal(1, p.GetValue<int>()),
            p => Assert.Equal(2, p.GetValue<int>()),
            p => Assert.Equal(3, p.GetValue<int>())
            );
    }
}