namespace OpaDotNet.Wasm;

public record BuiltinContext
{
    public string FunctionName { get; internal init; } = default!;

    public int OpaContext { get; internal init; }
}