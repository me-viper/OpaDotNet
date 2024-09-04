using System.Text.Json.Serialization;

namespace OpaDotNet.Wasm.Rego;

internal class RegoSetJsonConverterFactory : JsonConverterFactory
{
    public static RegoSetJsonConverterFactory Instance { get; } = new();

    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
            return false;

        return typeToConvert.GetGenericTypeDefinition() == typeof(RegoSet<>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var wrappedType = typeToConvert.GetGenericArguments()[0];

        var t = typeof(RegoSetJsonConverter<>).MakeGenericType(wrappedType);
        var converter = (JsonConverter?)Activator.CreateInstance(t);

        return converter;
    }
}

internal class RegoSetJsonConverter<T> : JsonConverter<RegoSet<T>>
{
    public override RegoSet<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        RegoSet<T>? result = null;

        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (!string.Equals(reader.GetString(), "__rego_set", StringComparison.Ordinal))
                continue;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    var ar = JsonSerializer.Deserialize<IEnumerable<T>>(ref reader, options);
                    result = new RegoSet<T>(ar ?? Array.Empty<T>());
                }
            }
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, RegoSet<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteStartObject();
        writer.WritePropertyName("__rego_set");
        writer.WriteStartArray();

        foreach (var e in value.Set)
        {
            var s = JsonSerializer.Serialize(e, options);
            writer.WriteRawValue(s);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndArray();
    }
}