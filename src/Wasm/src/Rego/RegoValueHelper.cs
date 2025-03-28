using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using OpaDotNet.Wasm.Internal;

namespace OpaDotNet.Wasm.Rego;

internal static class RegoValueHelper
{
    public static bool TryGetTuple<T>(this JsonArray ar, [MaybeNullWhen(false)] out Tuple<T> tuple)
    {
        ArgumentNullException.ThrowIfNull(ar);

        tuple = null;

        if (ar.Count < 1)
            return false;

        if (ar[0] is not JsonValue)
            return false;

        var val = ar[0]!.GetValue<T>();
        tuple = Tuple.Create(val);

        return true;
    }

    public static bool TryGetRegoSet<T>(
        this JsonNode? node,
        [MaybeNullWhen(false)] out RegoSet<T> set,
        JsonSerializerOptions? options = null)
    {
        set = null;

        if (node is not JsonArray ja)
            return false;

        return ja.TryGetRegoSet(out set, options);
    }

    public static bool TryGetRegoSetArray(
        this JsonArray ar,
        [MaybeNullWhen(false)] out JsonArray set)
    {
        ArgumentNullException.ThrowIfNull(ar);

        set = null;

        if (!ar.IsRegoSet())
            return false;

        try
        {
            set = ar[0]!["__rego_set"]!.AsArray();
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        return true;
    }

    public static bool TryGetRegoSet(
        this JsonArray ar,
        [MaybeNullWhen(false)] out RegoSet<JsonNode> set,
        JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(ar);

        set = null;

        if (!ar.IsRegoSet())
            return false;

        set = new RegoSet<JsonNode>(ar[0]!["__rego_set"]!.AsArray()!);

        return true;
    }

    public static bool TryGetRegoSet<T>(
        this JsonArray ar,
        [MaybeNullWhen(false)] out RegoSet<T> set,
        JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(ar);

        set = null;

        if (!ar.IsRegoSet())
            return false;

        set = ar[0]!.Deserialize<RegoSet<T>>(options);

        return set != null;
    }

    public static bool ContainsRegoSet(this JsonNode? node)
    {
        if (node == null)
            return false;

        if (node.IsRegoSet())
            return true;

        switch (node)
        {
            case JsonObject jo:
            {
                if (jo.Any(p => p.Value.ContainsRegoSet()))
                    return true;

                break;
            }
            case JsonArray ja:
            {
                if (ja.Any(p => p.ContainsRegoSet()))
                    return true;

                break;
            }
        }

        return false;
    }

    public static bool IsRegoSet(this JsonNode? node) => node is JsonArray ar && ar.IsRegoSet();

    public static bool IsRegoSet(this JsonArray? ar)
    {
        if (ar == null)
            return false;

        if (ar.Count != 1)
            return false;

        return ar[0] is JsonObject jo && jo.ContainsKey("__rego_set");
    }

    public static string JsonFromRegoValue(string s) => ConvertOpaJson(
        s,
        """[{"__rego_set":[]}]""",
        """[{"__rego_set":[""",
        "]}]"
        );

    public static string JsonToRegoValue(string s) => ConvertOpaJson(s, "set()", "{", "}");

    private static string ConvertOpaJson(
        ReadOnlySpan<char> s,
        ReadOnlySpan<char> emptySet,
        ReadOnlySpan<char> startSet,
        ReadOnlySpan<char> endSet)
    {
        var reader = new OpaJsonReader(s);

#pragma warning disable CA2000
        var sb = new ValueStringBuilder(s.Length);
#pragma warning restore CA2000

        try
        {
            Span<bool> writeSeparator = stackalloc bool[OpaJsonReader.MaxDepth];
            var depth = 0;

            while (reader.Read())
            {
                switch (reader.Token.TokenType)
                {
                    case OpaJsonTokenType.ObjectStart:
                    case OpaJsonTokenType.ArrayStart:
                    case OpaJsonTokenType.SetStart:
                        if (writeSeparator[depth])
                            sb.Append(',');
                        else
                            writeSeparator[depth] = true;

                        depth++;

                        if (depth > OpaJsonReader.MaxDepth)
                            throw new InvalidOperationException($"Maximum depth {OpaJsonReader.MaxDepth} reached");

                        break;

                    case OpaJsonTokenType.PropertyName:
                    case OpaJsonTokenType.Null:
                    case OpaJsonTokenType.EmptySet:
                    case OpaJsonTokenType.String:
                    case OpaJsonTokenType.Value:
                        if (writeSeparator[depth])
                            sb.Append(',');
                        else
                            writeSeparator[depth] = true;

                        break;

                    case OpaJsonTokenType.ObjectEnd:
                    case OpaJsonTokenType.ArrayEnd:
                    case OpaJsonTokenType.SetEnd:
                        writeSeparator[depth] = false;
                        depth--;
                        break;
                }

                switch (reader.Token.TokenType)
                {
                    case OpaJsonTokenType.ObjectStart:
                        sb.Append('{');
                        break;
                    case OpaJsonTokenType.PropertyName:
                        writeSeparator[depth] = false;
                        sb.AppendQuoted(reader.Token.Buf);
                        sb.Append(':');
                        break;
                    case OpaJsonTokenType.ObjectEnd:
                        sb.Append('}');
                        break;
                    case OpaJsonTokenType.ArrayStart:
                        sb.Append('[');
                        break;
                    case OpaJsonTokenType.ArrayEnd:
                        sb.Append(']');
                        break;
                    case OpaJsonTokenType.SetStart:
                        sb.Append(startSet);
                        break;
                    case OpaJsonTokenType.SetEnd:
                        sb.Append(endSet);
                        break;
                    case OpaJsonTokenType.Null:
                        sb.Append("null");
                        break;
                    case OpaJsonTokenType.EmptySet:
                        sb.Append(emptySet);
                        break;
                    case OpaJsonTokenType.String:
                        sb.AppendQuoted(reader.Token.Buf);
                        break;
                    case OpaJsonTokenType.Value:
                        sb.Append(reader.Token.Buf);
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected token: {reader.Token.TokenType}");
                }
            }

            return sb.ToString();
        }
        catch
        {
            sb.Dispose();
            throw;
        }
    }
}