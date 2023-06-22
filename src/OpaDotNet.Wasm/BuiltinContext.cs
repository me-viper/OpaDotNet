using JetBrains.Annotations;

namespace OpaDotNet.Wasm;

[PublicAPI]
public record BuiltinContext
{
    public string FunctionName { get; internal init; } = default!;

    public int OpaContext { get; internal init; }
}