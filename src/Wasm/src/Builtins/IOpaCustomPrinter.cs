namespace OpaDotNet.Wasm.Builtins;

/// <summary>
/// When implemented provides a way to customize printing from the policy evaluation.
/// </summary>
public interface IOpaCustomPrinter
{
    /// <summary>
    /// Called to emit a messages from the policy evaluation.
    /// </summary>
    /// <param name="args">Messages to emit.</param>
    void Print(IEnumerable<string> args);
}