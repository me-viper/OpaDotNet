using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

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

    public static void ToAlphabeticJsonBytes(
        this JsonNode? node,
        Stream stream,
        JsonSerializerOptions? options = default)
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