using System.Text.Json.Nodes;

using JetBrains.Annotations;

namespace OpaDotNet.Wasm;

[PublicAPI]
public class BuiltinArg
{
    private readonly Lazy<JsonNode?> _arg;

    private readonly JsonSerializerOptions _jsonOptions;

    internal BuiltinArg(Func<string> getArg, JsonSerializerOptions jsonOptions)
    {
        ArgumentNullException.ThrowIfNull(getArg);

        _arg = new Lazy<JsonNode?>(
            () =>
            {
                var json = getArg();
                return JsonNode.Parse(json);
            }
            );

        _jsonOptions = jsonOptions;
    }

    public JsonNode? Raw => _arg.Value;

    public T As<T>() where T : notnull
    {
        var result = AsOrNull<T>();

        if (result == null)
            throw new OpaEvaluationException("Argument is null");

        return result;
    }

    public T? AsOrNull<T>(Func<T>? defaultValue = null)
    {
        return Raw switch
        {
            null => defaultValue != null ? defaultValue() : default,
            JsonValue jv => jv.GetValue<T>(),
            _ => Raw.Deserialize<T>(_jsonOptions),
        };
    }
}