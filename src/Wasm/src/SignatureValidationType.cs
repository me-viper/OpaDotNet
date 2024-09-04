namespace OpaDotNet.Wasm;

/// <summary>
/// Signature validation type.
/// </summary>
public enum SignatureValidationType
{
    /// <summary>
    /// If bundle is not signed no validation performed.
    /// If bundle is signed validation is performed.
    /// </summary>
    Default,

    /// <summary>
    /// Skip signature validation.
    /// </summary>
    Skip,

    /// <summary>
    /// Require signature validation.
    /// </summary>
    Required
}