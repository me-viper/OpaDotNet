using System.Text.Json.Nodes;

using JetBrains.Annotations;

namespace OpaDotNet.Wasm;

[PublicAPI]
public class BuiltinArg
{
    private readonly Lazy<JsonNode?> _arg;

    internal BuiltinArg(Func<string> getArg)
    {
        ArgumentNullException.ThrowIfNull(getArg);

        _arg = new Lazy<JsonNode?>(
            () =>
            {
                var json = getArg();
                return JsonNode.Parse(json);
            });
    }

    public JsonNode? Raw => _arg.Value;

    public T AsOrFail<T>() where T : notnull
    {
        var result = As<T>();

        if (result == null)
            throw new OpaEvaluationException("Argument is null");

        return result;
    }

    public T? As<T>(Func<T>? defaultValue = null)
    {
        return Raw switch
        {
            null => defaultValue != null ? defaultValue() : default,
            JsonValue jv => jv.GetValue<T>(),
            _ => Raw.Deserialize<T>(),
        };
    }
}