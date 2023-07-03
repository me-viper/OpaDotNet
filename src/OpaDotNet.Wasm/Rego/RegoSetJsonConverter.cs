using System.Text.Json.Serialization;

namespace OpaDotNet.Wasm.Rego;

internal class RegoSetJsonConverterFactory : JsonConverterFactory
{
    public static RegoSetJsonConverterFactory Instance { get; } = new();

    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert == typeof(RegoSetOfAny))
            return true;

        if (!typeToConvert.IsGenericType)
            return false;

        return typeToConvert.GetGenericTypeDefinition() == typeof(RegoSet<>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (typeToConvert == typeof(RegoSetOfAny))
            return new RegoSetJsonConverter<object>();

        var wrappedType = typeToConvert.GetGenericArguments()[0];

        var t = typeof(RegoSetJsonConverter<>).MakeGenericType(wrappedType);
        var converter = (JsonConverter?)Activator.CreateInstance(t);

        return converter;
    }
}

internal class RegoSetJsonConverter<T> : JsonConverter<RegoSet<T>>
{
    public override RegoSet<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, RegoSet<T> value, JsonSerializerOptions options)
    {
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
    }
}