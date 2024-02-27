using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Nodes;

using JetBrains.Annotations;

namespace OpaDotNet.Wasm;

/// <summary>
/// Represents built-in function argument.
/// </summary>
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
    /// JSON with REGO sets specific patches.
    /// </summary>
    public JsonNode? Raw => _arg(RegoValueFormat.Value);

    /// <summary>
    /// Raw JSON with REGO sets serialized as arrays.
    /// </summary>
    public JsonNode? RawJson => _arg(RegoValueFormat.Json);

    /// <summary>
    /// Converts built-in function argument in JSON format to the specified type.
    /// </summary>
    /// <param name="format">Value type JSON.</param>
    /// <typeparam name="T">Target type.</typeparam>
    public T As<T>(RegoValueFormat format = RegoValueFormat.Json) where T : notnull
    {
        var result = AsOrNull<T>(null, format);

        if (result == null)
            throw new OpaEvaluationException("Argument is null");

        return result;
    }

    private static MethodInfo _getValue = typeof(JsonValue).GetMethod(nameof(JsonValue.GetValue))!;

    private readonly ConcurrentDictionary<Type, MethodInfo> _getValueCache = new();

    internal object? As(Type type, RegoValueFormat format = RegoValueFormat.Json)
    {
        var val = format == RegoValueFormat.Value ? Raw : RawJson;

        if (val == null)
            return null;

        if (val.GetType().IsAssignableTo(type))
            return val;

        if (val is JsonValue jv)
        {
            var fun = _getValueCache.GetOrAdd(type, _getValue.MakeGenericMethod(type));
            return fun.Invoke(jv, null);
        }

        return Raw.Deserialize(type, _jsonOptions);
    }

    /// <summary>
    /// Converts built-in function argument in JSON format to the specified type.
    /// </summary>
    /// <param name="defaultValue">Value to return if built-in is null.</param>
    /// <param name="format">
    /// If <c>RegoValueFormat.Json</c> <see cref="RawJson"/> will be used as source for the conversion.
    /// If <c>RegoValueFormat.Value</c> <see cref="Raw"/> will be used as source for the conversion.
    /// </param>
    /// <typeparam name="T">Target type.</typeparam>
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