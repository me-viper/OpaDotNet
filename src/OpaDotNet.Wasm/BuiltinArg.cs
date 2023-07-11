using System.Text.Json.Nodes;

using JetBrains.Annotations;

namespace OpaDotNet.Wasm;

[PublicAPI]
public class BuiltinArg
{
    private readonly Func<EvaluationOutputFormat, JsonNode?> _arg;

    private readonly JsonSerializerOptions _jsonOptions;

    internal BuiltinArg(Func<EvaluationOutputFormat, string> getArg, JsonSerializerOptions jsonOptions)
    {
        ArgumentNullException.ThrowIfNull(getArg);

        _arg = p =>
        {
            var json = getArg(p);
            return JsonNode.Parse(json);
        };

        _jsonOptions = jsonOptions;
    }

    /// <summary>
    /// Json with REGO sets specific patches.
    /// </summary>
    public JsonNode? Raw => _arg(EvaluationOutputFormat.Value);

    /// <summary>
    /// Raw Json with REGO sets serialized as arrays.
    /// </summary>
    public JsonNode? RawJson => _arg(EvaluationOutputFormat.Json);

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
            T t => t,
            _ => Raw.Deserialize<T>(_jsonOptions),
        };
    }
}