using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Json.More;

namespace OpaDotNet.Wasm.Internal;

internal static class JsonExtensions
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        Converters = { AlphabeticJsonNodeConverter.Instance },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // public static string ToAlphabeticJsonString(this JsonNode? node, JsonSerializerOptions? options = default)
    // {
    //     if (options == null)
    //         options = DefaultOptions;
    //     else
    //     {
    //         options = new JsonSerializerOptions(options);
    //         options.Converters.Insert(0, AlphabeticJsonNodeConverter.Instance);
    //     }
    //
    //     return JsonSerializer.Serialize(node, options);
    // }

    public static bool TryGetArray<T>(this JsonNode? node, [MaybeNullWhen(false)] out IReadOnlyList<T> result)
    {
        result = null;

        if (node == null)
            return false;

        result = node switch
        {
            JsonArray array => new List<T>(array.GetValues<T>()),
            JsonValue value => [value.GetValue<T>()],
            _ => null,
        };

        return result != null;
    }

    public static void ToAlphabeticJsonBytes(
        this JsonNode? node,
        Stream stream,
        JsonSerializerOptions? options = null)
    {
        if (options == null)
            options = DefaultOptions;
        else
        {
            options = new JsonSerializerOptions(options);
            options.Converters.Insert(0, AlphabeticJsonNodeConverter.Instance);
        }

        JsonSerializer.Serialize(stream, node, options);
    }

    public static bool IsEquivalentTo(this JsonNode? a, JsonNode? b, bool strictArrayOrder)
    {
        // if (a == null && b == null)
        //     return true;

        if (strictArrayOrder)
            return a?.IsEquivalentTo(b) ?? false;

        switch (a, b)
        {
            case (null, null):
                return true;

            case (JsonObject objA, JsonObject objB):
                if (objA.Count != objB.Count)
                    return false;

                var grouped = objA.Concat(objB)
                    .GroupBy(p => p.Key)
                    .Select(g => g.ToList())
                    .ToList();

                return grouped.All(p => p.Count == 2 && p[0].Value.IsEquivalentTo(p[1].Value, strictArrayOrder));

            case (JsonArray arrayA, JsonArray arrayB):
                if (arrayA.Count != arrayB.Count)
                    return false;

                var matches = new bool[arrayB.Count];

                foreach (var el in arrayA)
                {
                    var match = false;

                    for (var j = 0; j < arrayB.Count; j++)
                    {
                        if (matches[j])
                            continue;

                        if (el.IsEquivalentTo(arrayB[j], strictArrayOrder))
                        {
                            matches[j] = true;
                            match = true;
                            break;
                        }
                    }

                    if (!match)
                        return false;
                }

                return true;

            case (JsonValue aValue, JsonValue bValue):
                var aNumber = aValue.GetNumber();
                var bNumber = bValue.GetNumber();

                if (aNumber != null)
                    return aNumber == bNumber;

                var aString = aValue.GetString();
                var bString = bValue.GetString();

                if (aString != null)
                    return aString == bString;

                var aBool = aValue.GetBool();
                var bBool = bValue.GetBool();

                if (aBool.HasValue)
                    return aBool == bBool;

                var aObj = aValue.GetValue<object>();
                var bObj = bValue.GetValue<object>();

                if (aObj is JsonElement aElement && bObj is JsonElement bElement)
                    return aElement.IsEquivalentTo(bElement);

                return aObj.Equals(bObj);

            default:
                return false;
        }
    }
}

internal class LaxJsonNodeEqualityComparer : IEqualityComparer<JsonNode?>
{
    public static LaxJsonNodeEqualityComparer Instance { get; } = new();

    public bool Equals(JsonNode? x, JsonNode? y) => x.IsEquivalentTo(y, false);

    public int GetHashCode(JsonNode obj) => obj.GetEquivalenceHashCode();
}

internal class AlphabeticJsonNodeConverter : JsonConverter<JsonNode>
{
    public static AlphabeticJsonNodeConverter Instance { get; } = new();

    public override bool CanConvert(Type typeToConvert)
        => typeof(JsonNode).IsAssignableFrom(typeToConvert) && typeToConvert != typeof(JsonValue);

    public override JsonNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => JsonNode.Parse(ref reader);

    public override void Write(Utf8JsonWriter writer, JsonNode? value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case JsonObject obj:
                writer.WriteStartObject();

                foreach (var pair in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(pair.Key);
                    Write(writer, pair.Value, options);
                }

                writer.WriteEndObject();

                break;

            case JsonArray array: // We need to handle JsonArray explicitly to ensure that objects inside arrays are alphabetized
                writer.WriteStartArray();

                foreach (var item in array)
                    Write(writer, item, options);

                writer.WriteEndArray();

                break;

            case null:
                writer.WriteNullValue();
                break;

            default: // JsonValue
                value.WriteTo(writer, options);
                break;
        }
    }
}