using JetBrains.Annotations;

namespace OpaDotNet.Wasm;

/// <summary>
/// Built-in function execution context.
/// </summary>
[PublicAPI]
public record BuiltinContext
{
    /// <summary>
    /// Built-in function name.
    /// </summary>
    public string FunctionName { get; internal init; } = default!;

    /// <summary>
    /// Reserved for future use.
    /// </summary>
    public int OpaContext { get; internal init; }

    /// <summary>
    /// JSON serialization options.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; internal init; } = default!;
}