using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Json.More;
using Json.Patch;
using Json.Pointer;
using Json.Schema;

namespace OpaDotNet.Wasm;

public partial class DefaultOpaImportsAbi
{
    private sealed class LaxJsonPointerJsonConverter : WeaklyTypedJsonConverter<JsonPointer>
    {
        private readonly JsonPointerJsonConverter _inner = new();

        public override JsonPointer? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("Expected string");

            var str = reader.GetString()!;

            if (str.Length > 0 && str[0] != '#')
            {
                if (str[0] != '/')
                    str = '/' + str;
            }

            return JsonPointer.TryParse(str, out var pointer)
                ? pointer
                : throw new JsonException("Value does not represent a JSON Pointer");
        }

        public override void Write(Utf8JsonWriter writer, JsonPointer value, JsonSerializerOptions options)
        {
            _inner.Write(writer, value, options);
        }
    }

    private static readonly JsonSerializerOptions PatchOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        //Converters = { new LaxJsonPointerJsonConverter() },
    };

    private static JsonNode? JsonPatch(JsonNode? obj, JsonNode? patches)
    {
        if (patches == null)
            return obj;

        var ops = patches.Deserialize<PatchOperation[]>(PatchOptions);

        if (ops == null)
            return obj;

        var p = new JsonPatch(ops);
        var result = p.Apply(obj);

        return result.Result;
    }

    private static object?[] JsonVerifySchema(JsonNode? schema, JsonSerializerOptions options, out JsonSchema? result)
    {
        static object?[] Success() => [true, null];
        static object?[] Fail(string message) => [false, message];

        result = null;

        if (schema == null)
            return Success();

        try
        {
            string? schemaString;

            if (schema is JsonValue jv)
                jv.TryGetValue(out schemaString);
            else
                schemaString = schema.ToJsonString(options);

            if (string.IsNullOrWhiteSpace(schemaString))
                return Fail("Invalid schema");

            result = JsonSchema.FromText(schemaString);

            return Success();
        }
        catch (JsonException ex)
        {
            return Fail(ex.Message);
        }
    }

    private class JsonSchemaError
    {
        [JsonPropertyName("desc")]
        [UsedImplicitly]
        public string? Description { get; set; }

        [JsonPropertyName("error")]
        [UsedImplicitly]
        public string? Error { get; set; }

        [JsonPropertyName("field")]
        [UsedImplicitly]
        public string? Filed { get; set; }

        [JsonPropertyName("type")]
        [UsedImplicitly]
        public string? Type { get; set; }
    }

    private static object?[]? JsonMatchSchema(JsonNode? document, JsonNode? schema, JsonSerializerOptions options)
    {
        JsonNode? doc;

        if (document is not JsonValue jv)
        {
            if (document is not JsonObject)
                return null;

            doc = document;
        }
        else
        {
            if (!jv.TryGetValue<string>(out var s))
                return null;

            try
            {
                doc = JsonNode.Parse(s);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        if (doc == null)
            return null;

        static object?[] Success() => [true, Array.Empty<object>()];
        static object?[] Fail(params JsonSchemaError[] errors) => [false, errors];

        JsonVerifySchema(schema, options, out var sch);

        if (sch == null)
            return null;

        var result = sch.Evaluate(document, new() { OutputFormat = OutputFormat.List });

        if (result.IsValid)
            return Success();

        var errors = new List<JsonSchemaError>();

        foreach (var detail in result.Details)
        {
            if (detail.IsValid)
                continue;

            var e = detail.Errors?.FirstOrDefault();

            if (!detail.HasErrors || e == null)
                continue;

            var err = new JsonSchemaError
            {
                Filed = detail.EvaluationPath.ToString(),
                Type = e.Value.Key,
                Error = e.Value.Value,
            };

            errors.Add(err);
        }

        return Fail(errors.ToArray());
    }
}