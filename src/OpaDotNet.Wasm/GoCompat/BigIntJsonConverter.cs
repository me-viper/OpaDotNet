using System.Globalization;
using System.Text.Json.Serialization;

namespace OpaDotNet.Wasm.GoCompat;

internal class BigIntJsonConverter : JsonConverter<BigIntJson>
{
    public override BigIntJson? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, BigIntJson value, JsonSerializerOptions options)
    {
        writer.WriteRawValue(value.N.ToString("e15", CultureInfo.InvariantCulture));
    }
}