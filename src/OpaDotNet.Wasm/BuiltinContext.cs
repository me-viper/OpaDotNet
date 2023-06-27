using JetBrains.Annotations;

namespace OpaDotNet.Wasm;

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
}