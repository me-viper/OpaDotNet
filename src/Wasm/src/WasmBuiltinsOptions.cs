using OpaDotNet.Wasm.Builtins;

namespace OpaDotNet.Wasm;

/// <summary>
/// OPA built-ins configuration.
/// </summary>
public class WasmBuiltinsOptions
{
    /// <summary>
    /// Default OPA built-ins.
    /// </summary>
    public IOpaImportsAbi DefaultBuiltins { get; set; } = new DefaultOpaImportsAbi();

    /// <summary>
    /// Custom OPA built-ins.
    /// </summary>
    public IList<IOpaCustomBuiltins> CustomBuiltins { get; } = [];
}