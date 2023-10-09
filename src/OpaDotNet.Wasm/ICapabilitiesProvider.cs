namespace OpaDotNet.Wasm;

/// <summary>
/// Exposes OPA built-ins implementation capabilities.
/// </summary>
public interface ICapabilitiesProvider
{
    /// <summary>
    /// Stream containing capabilities JSON.
    /// </summary>
    /// <returns>Capabilities JSON.</returns>
    Stream GetCapabilities();
}