using OpaDotNet.Wasm.Rego;

namespace OpaDotNet.Wasm.Tests;

public class OpaJsonReaderTests
{
    [Theory]
    [InlineData("10")]
    [InlineData("-10.2e15")]
    [InlineData("10.2E15")]
    [InlineData("0010")]
    [InlineData("true")]
    [InlineData("false")]
    public void ReadValue(string val)
    {
        var reader = new OpaJsonReader(val);

        AssertJsonReader.Read(reader, AssertJsonReader.Value(val));
    }

    [Fact]
    public void ReadNull()
    {
        var reader = new OpaJsonReader("null");

        Assert.True(reader.Read());
        Assert.Equal(OpaJsonTokenType.Null, reader.Token.TokenType);
        Assert.True(reader.Token.Buf.IsEmpty);
        Assert.False(reader.Read());
    }

    [Fact]
    public void ReadSet()
    {
        var reader = new OpaJsonReader("""{"a", 2}""");

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.SetStart),
            AssertJsonReader.String("a"),
            AssertJsonReader.Value("2"),
            AssertJsonReader.Token(OpaJsonTokenType.SetEnd)
            );
    }

    [Fact]
    public void ReadSet2()
    {
        var reader = new OpaJsonReader("""{2, "a"}""");

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.SetStart),
            AssertJsonReader.Value("2"),
            AssertJsonReader.String("a"),
            AssertJsonReader.Token(OpaJsonTokenType.SetEnd)
            );
    }

    [Fact]
    public void ReadEmptySet()
    {
        var reader = new OpaJsonReader("""{"a": set()}""");

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.ObjectStart),
            AssertJsonReader.Property("a"),
            AssertJsonReader.Token(OpaJsonTokenType.EmptySet),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectEnd)
            );
    }

    [Fact]
    public void ReadEmptyArray()
    {
        var reader = new OpaJsonReader("""[]""");

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.ArrayStart),
            AssertJsonReader.Token(OpaJsonTokenType.ArrayEnd)
            );
    }

    [Fact]
    public void ReadEmptyObject()
    {
        var reader = new OpaJsonReader("""{"a": {}}""");

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.ObjectStart),
            AssertJsonReader.Property("a"),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectStart),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectEnd),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectEnd)
            );
    }

    [Fact]
    public void ReadSet3()
    {
        var reader = new OpaJsonReader("""{{{1, 2, "aaa"}}}""");

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.SetStart),
            AssertJsonReader.Token(OpaJsonTokenType.SetStart),
            AssertJsonReader.Token(OpaJsonTokenType.SetStart),
            AssertJsonReader.Value("1"),
            AssertJsonReader.Value("2"),
            AssertJsonReader.String("aaa"),
            AssertJsonReader.Token(OpaJsonTokenType.SetEnd),
            AssertJsonReader.Token(OpaJsonTokenType.SetEnd),
            AssertJsonReader.Token(OpaJsonTokenType.SetEnd)
            );
    }

    [Fact]
    public void ReadSet4()
    {
        var reader = new OpaJsonReader("""{{{{"xx": 1}}}}""");

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.SetStart),
            AssertJsonReader.Token(OpaJsonTokenType.SetStart),
            AssertJsonReader.Token(OpaJsonTokenType.SetStart),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectStart),
            AssertJsonReader.Property("xx"),
            AssertJsonReader.Value("1"),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectEnd),
            AssertJsonReader.Token(OpaJsonTokenType.SetEnd),
            AssertJsonReader.Token(OpaJsonTokenType.SetEnd),
            AssertJsonReader.Token(OpaJsonTokenType.SetEnd)
            );
    }

    [Fact]
    public void ReadSet5()
    {
        var reader = new OpaJsonReader("""{1,{"a":{"y","z"}}}""");

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.SetStart),
            AssertJsonReader.Value("1"),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectStart),
            AssertJsonReader.Property("a"),
            AssertJsonReader.Token(OpaJsonTokenType.SetStart),
            AssertJsonReader.String("y"),
            AssertJsonReader.String("z"),
            AssertJsonReader.Token(OpaJsonTokenType.SetEnd),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectEnd),
            AssertJsonReader.Token(OpaJsonTokenType.SetEnd)
            );
    }

    [Fact]
    public void ReadSetOfObjects()
    {
        var reader = new OpaJsonReader("""{{"aa": 11, "xx": false}, {"bb": "333"}, 2222}""");

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.SetStart),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectStart),
            AssertJsonReader.Property("aa"),
            AssertJsonReader.Value("11"),
            AssertJsonReader.Property("xx"),
            AssertJsonReader.Value("false"),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectEnd),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectStart),
            AssertJsonReader.Property("bb"),
            AssertJsonReader.String("333"),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectEnd),
            AssertJsonReader.Value("2222"),
            AssertJsonReader.Token(OpaJsonTokenType.SetEnd)
            );
    }

    [Fact]
    public void ReadEmptySetShim()
    {
        var reader = new OpaJsonReader("""[{"__rego_set":[]}]""");

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.EmptySet)
            );
    }

    [Fact]
    public void ReadSetShim()
    {
        var reader = new OpaJsonReader("""[{"__rego_set":["a", 2]}]""");

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.SetStart),
            AssertJsonReader.String("a"),
            AssertJsonReader.Value("2"),
            AssertJsonReader.Token(OpaJsonTokenType.SetEnd)
            );
    }

    [Fact]
    public void ReadSetShim2()
    {
        var reader = new OpaJsonReader("""[{"__rego_set":[{"a": 2, "c": "test"}, "b"]}]""");

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.SetStart),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectStart),
            AssertJsonReader.Property("a"),
            AssertJsonReader.Value("2"),
            AssertJsonReader.Property("c"),
            AssertJsonReader.String("test"),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectEnd),
            AssertJsonReader.String("b"),
            AssertJsonReader.Token(OpaJsonTokenType.SetEnd)
            );
    }

    [Fact]
    public void ReadArrayOfObjects()
    {
        var reader = new OpaJsonReader("""[{"a":["a", 2]}]""");

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.ArrayStart),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectStart),
            AssertJsonReader.Property("a"),
            AssertJsonReader.Token(OpaJsonTokenType.ArrayStart),
            AssertJsonReader.String("a"),
            AssertJsonReader.Value("2"),
            AssertJsonReader.Token(OpaJsonTokenType.ArrayEnd),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectEnd),
            AssertJsonReader.Token(OpaJsonTokenType.ArrayEnd)
            );
    }

    [Fact]
    public void ReadObject()
    {
        var reader = new OpaJsonReader("""{"a":1,"b":"2"}""");

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.ObjectStart),
            AssertJsonReader.Property("a"),
            AssertJsonReader.Value("1"),
            AssertJsonReader.Property("b"),
            AssertJsonReader.String("2"),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectEnd)
            );
    }

    [Fact]
    public void ReadNestedObject()
    {
        var reader = new OpaJsonReader("""{"a":{"b":"2"}}""");

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.ObjectStart),
            AssertJsonReader.Property("a"),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectStart),
            AssertJsonReader.Property("b"),
            AssertJsonReader.String("2"),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectEnd),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectEnd)
            );
    }

    [Fact]
    public void ReadObjectInArray()
    {
        var reader = new OpaJsonReader("""{"a":[{"b":"2"},{"c": 1}]}""");

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.ObjectStart),
            AssertJsonReader.Property("a"),
            AssertJsonReader.Token(OpaJsonTokenType.ArrayStart),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectStart),
            AssertJsonReader.Property("b"),
            AssertJsonReader.String("2"),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectEnd),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectStart),
            AssertJsonReader.Property("c"),
            AssertJsonReader.Value("1"),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectEnd),
            AssertJsonReader.Token(OpaJsonTokenType.ArrayEnd),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectEnd)
            );
    }

    [Fact]
    public void ReadObjectInArray2()
    {
        var reader = new OpaJsonReader("""{"a":[{"b":"2"},"c",1]}""");

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.ObjectStart),
            AssertJsonReader.Property("a"),
            AssertJsonReader.Token(OpaJsonTokenType.ArrayStart),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectStart),
            AssertJsonReader.Property("b"),
            AssertJsonReader.String("2"),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectEnd),
            AssertJsonReader.String("c"),
            AssertJsonReader.Value("1"),
            AssertJsonReader.Token(OpaJsonTokenType.ArrayEnd),
            AssertJsonReader.Token(OpaJsonTokenType.ObjectEnd)
            );
    }

    [Theory]
    [InlineData("aaa")]
    [InlineData("aa\nbbb")]
    [InlineData("aa  bbb")]
    public void ReadString(string val)
    {
        var reader = new OpaJsonReader($"\"{val}\"");
        AssertJsonReader.Read(reader, AssertJsonReader.String(val));
    }

    [Theory]
    [InlineData("[1,2]")]
    [InlineData("[ 1 , 2 ]")]
    public void ReadArray(string val)
    {
        var reader = new OpaJsonReader(val);

        AssertJsonReader.Read(
            reader,
            AssertJsonReader.Token(OpaJsonTokenType.ArrayStart),
            AssertJsonReader.Value("1"),
            AssertJsonReader.Value("2"),
            AssertJsonReader.Token(OpaJsonTokenType.ArrayEnd)
            );
    }
}

internal static class AssertJsonReader
{
    public static Func<(OpaJsonTokenType, string?)> Token(OpaJsonTokenType t) => () => (t, null);

    public static Func<(OpaJsonTokenType, string?)> Value(string val) => () => (OpaJsonTokenType.Value, val);

    public static Func<(OpaJsonTokenType, string?)> String(string val) => () => (OpaJsonTokenType.String, val);

    public static Func<(OpaJsonTokenType, string?)> Property(string val) => () => (OpaJsonTokenType.PropertyName, val);

    public static void Read(OpaJsonReader reader, params Func<(OpaJsonTokenType, string?)>[] expected)
    {
        var asserts = new Action<(OpaJsonTokenType, string)>[expected.Length];

        for (var i = 0; i < asserts.Length; i++)
        {
            var (type, val) = expected[i]();
            asserts[i] = p =>
            {
                Assert.Equal(type, p.Item1);

                if (val != null)
                    Assert.Equal(val, p.Item2);
            };
        }

        var tokens = new List<(OpaJsonTokenType, string)>();

        while (reader.Read())
            tokens.Add((reader.Token.TokenType, reader.Token.Buf.ToString()));

        Assert.Collection(tokens, asserts);
    }
}