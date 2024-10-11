namespace OpaDotNet.Wasm.Builtins;

/// <summary>
/// Custom OPA built-ins.
/// </summary>
public interface IOpaCustomBuiltins
{
    /// <summary>
    /// Resets built-ins between evaluations.
    /// </summary>
    void Reset()
    {
    }
}