using System.Text.Json.Nodes;

using JetBrains.Annotations;

namespace OpaDotNet.Wasm;

[PublicAPI]
public class BuiltinArg
{
    private readonly Func<RegoValueFormat, JsonNode?> _arg;

    private readonly JsonSerializerOptions _jsonOptions;

    internal BuiltinArg(Func<RegoValueFormat, string> getArg, JsonSerializerOptions jsonOptions)
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
    public JsonNode? Raw => _arg(RegoValueFormat.Value);

    /// <summary>
    /// Raw Json with REGO sets serialized as arrays.
    /// </summary>
    public JsonNode? RawJson => _arg(RegoValueFormat.Json);

    public T As<T>(RegoValueFormat format = RegoValueFormat.Json) where T : notnull
    {
        var result = AsOrNull<T>(null, format);

        if (result == null)
            throw new OpaEvaluationException("Argument is null");

        return result;
    }

    public T? AsOrNull<T>(Func<T>? defaultValue = null, RegoValueFormat format = RegoValueFormat.Json)
    {
        var val = format == RegoValueFormat.Value ? Raw : RawJson;
        
        return val switch
        {
            null => defaultValue != null ? defaultValue() : default,
            JsonValue jv => jv.GetValue<T>(),
            T t => t,
            _ => Raw.Deserialize<T>(_jsonOptions),
        };
    }
}