using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Json.More;
using Json.Patch;
using Json.Pointer;
using Json.Schema;

using OpaDotNet.Wasm.Rego;

namespace OpaDotNet.Wasm;

public partial class DefaultOpaImportsAbi
{
    private sealed class LaxJsonPointerJsonConverter : WeaklyTypedJsonConverter<JsonPointer>
    {
        private readonly JsonPointerJsonConverter _inner = new();

        public override JsonPointer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("Expected string");

            var str = reader.GetString();

            if (str == null)
                throw new JsonException("Value does not represent a JSON Pointer");

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
        Converters = { new LaxJsonPointerJsonConverter() },
    };

    private static JsonNode? JsonPatch(JsonNode? obj, JsonNode? patches)
    {
        if (patches == null)
            return obj;

        var ops = patches.Deserialize<PatchOperation[]>(PatchOptions);

        if (ops == null)
            return obj;

        if (obj.ContainsRegoSet())
        {
            for (var i = 0; i < ops.Length; i++)
                ops[i] = AdjustPathForRegoSet(ops[i], obj);
        }

        var p = new JsonPatch(ops);
        var result = p.Apply(obj);

        if (!result.IsSuccess)
        {
            var err = result.Error ?? "Failed to apply patch";
            throw new OpaBuiltinException(err);
        }

        return result.Result;
    }

    private static PatchOperation AdjustPathForRegoSet(PatchOperation operation, JsonNode? node)
    {
        if (node == null)
            return operation;

        var from = AdjustPathForRegoSet(operation.From, node);

        switch (operation.Op)
        {
            case OperationType.Unknown:
                return operation;
            case OperationType.Add:
                var addPath = AdjustPathForRegoSet(operation.Path, node, false);
                return PatchOperation.Add(addPath, operation.Value);
            case OperationType.Remove:
                var removePath = AdjustPathForRegoSet(operation.Path, node);
                return PatchOperation.Remove(removePath);
            case OperationType.Replace:
                var replacePath = AdjustPathForRegoSet(operation.Path, node);
                return PatchOperation.Replace(replacePath, operation.Value);
            case OperationType.Move:
                var movePath = AdjustPathForRegoSet(operation.Path, node, false);
                return PatchOperation.Move(from, movePath);
            case OperationType.Copy:
                var copyTargetPath = AdjustPathForRegoSet(operation.Path, node, false);
                return PatchOperation.Copy(from, copyTargetPath);
            case OperationType.Test:
                var testPath = AdjustPathForRegoSet(operation.Path, node, false);
                return PatchOperation.Test(testPath, operation.Value);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static JsonPointer AdjustPathForRegoSet(JsonPointer pointer, JsonNode node, bool targetMustExist = true)
    {
        if (pointer.Count == 0)
            return pointer;

        var result = JsonPointer.Empty;
        JsonElement? currentElement = node.ToJsonDocument().RootElement;

        for (var i = 0; i < pointer.Count; i++)
        {
            result = result.Combine(pointer[i]);
            currentElement = JsonPointer.Create(pointer[i]).Evaluate(currentElement.Value);

            if (currentElement == null)
                throw new InvalidOperationException($"Target path '{result}' could not be reached");

            if (!currentElement.Value.AsNode().IsRegoSet())
                continue;

            if (i + 1 >= pointer.Count)
                continue;

            var setPointer = JsonPointer.Create(0, "__rego_set");
            currentElement = setPointer.Evaluate(currentElement.Value);

            if (currentElement == null)
                throw new InvalidOperationException("Invalid set");

            i++;
            var setMember = pointer[i];
            var foundPosition = 0;

            if (targetMustExist)
            {
                foundPosition = -1;

                for (var j = 0; j < currentElement.Value.GetArrayLength(); j++)
                {
                    if (currentElement.Value[j].ValueEquals(setMember))
                    {
                        currentElement = currentElement.Value[j];
                        foundPosition = j;
                        break;
                    }
                }

                if (foundPosition < 0)
                    throw new InvalidOperationException($"Set does not contain '{setMember}' value");
            }

            result = result.Combine(setPointer);
            result = result.Combine(foundPosition);
        }

        return result;
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
            throw new FormatException("Invalid json schema");

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

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private class JsonMarshalOptions
    {
        [JsonPropertyName("pretty")]
        public bool? Pretty { get; set; }

        [JsonPropertyName("prefix")]
        public string Prefix { get; set; } = string.Empty;

        [JsonPropertyName("indent")]
        public string Indent { get; set; } = "\t";
    }

    private static string MarshalWithOptions(string x, JsonMarshalOptions opts)
    {
        if (opts.Pretty == false)
            return x;

        var doPretty = opts.Prefix != string.Empty || opts.Indent != "\t";

        if (!doPretty && opts.Pretty != true)
            return x;

        var result = new StringBuilder(x.Length);

        var bytes = Encoding.UTF8.GetBytes(x);
        var reader = new Utf8JsonReader(bytes);

        var indent = 0;
        var lastToken = JsonTokenType.None;

        if (!string.IsNullOrWhiteSpace(opts.Prefix))
            result.Append(opts.Prefix);

        while (reader.Read())
        {
            var doIndent = lastToken is not (JsonTokenType.PropertyName or JsonTokenType.None);
            var doComa = lastToken is not (JsonTokenType.StartArray or JsonTokenType.StartObject);

            if (reader.TokenType is JsonTokenType.EndArray or JsonTokenType.EndObject)
            {
                indent--;
                doComa = false;

                if (lastToken is JsonTokenType.StartArray or JsonTokenType.StartObject)
                    doIndent = false;
            }

            if (doIndent)
            {
                if (doComa)
                    result.Append(',');

                result.Append('\n');

                if (!string.IsNullOrWhiteSpace(opts.Prefix))
                    result.Append(opts.Prefix);

                for (var i = 0; i < indent; i++)
                    result.Append(opts.Indent);
            }

            switch (reader.TokenType)
            {
                case JsonTokenType.StartArray:
                    result.Append('[');
                    indent++;
                    break;
                case JsonTokenType.EndArray:
                    result.Append(']');
                    break;
                case JsonTokenType.StartObject:
                    result.Append('{');
                    indent++;
                    break;
                case JsonTokenType.EndObject:
                    result.Append('}');
                    break;
                case JsonTokenType.PropertyName:
                {
                    var v = Encoding.UTF8.GetString(reader.ValueSpan);
                    result.Append($"\"{v}\": ");
                    break;
                }
                case JsonTokenType.String:
                {
                    var v = Encoding.UTF8.GetString(reader.ValueSpan);
                    result.Append($"\"{v}\"");
                    break;
                }
                case JsonTokenType.False or JsonTokenType.True or JsonTokenType.Null or JsonTokenType.Number:
                {
                    var v = Encoding.UTF8.GetString(reader.ValueSpan);
                    result.Append(v);
                    break;
                }
            }

            lastToken = reader.TokenType;
        }

        return result.ToString();
    }
}