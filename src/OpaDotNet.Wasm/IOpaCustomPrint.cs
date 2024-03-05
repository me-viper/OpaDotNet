namespace OpaDotNet.Wasm;

/// <summary>
/// When implemented provides a way to customize printing from the policy evaluation.
/// </summary>
public interface IOpaCustomPrint
{
    /// <summary>
    /// Called to emit a messages from the policy evaluation.
    /// </summary>
    /// <param name="args">Messages to emit.</param>
    void Print(IEnumerable<string> args);
}