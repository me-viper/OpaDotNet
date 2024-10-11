namespace OpaDotNet.Wasm;

/// <summary>
/// Creates built-ins implementation instances.
/// </summary>
public interface IBuiltinsFactory
{
    /// <summary>
    /// Creates built-ins implementation instances.
    /// </summary>
    IOpaImportsAbi Create();
}