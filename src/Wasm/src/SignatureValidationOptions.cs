using OpaDotNet.Wasm.Validation;

namespace OpaDotNet.Wasm;

/// <summary>
/// Contains members that affect OPA bundle signature validation.
/// </summary>
[PublicAPI]
public class SignatureValidationOptions
{
    /// <summary>
    /// Defines validation to perform.
    /// </summary>
    public SignatureValidationType Validation { get; init; }

    /// <summary>
    /// File names to exclude during bundle verification.
    /// </summary>
    public IReadOnlySet<string> ExcludeFiles { get; init; } = new HashSet<string>();

    /// <summary>
    /// Name of the signing algorithm (default "RS256").
    /// </summary>
    public string SigningAlgorithm { get; init; } = "RS256";

    /// <summary>
    /// Scope to use for bundle signature verification.
    /// </summary>
    internal string? Scope { get; init; }

    /// <summary>
    /// Secret (HMAC) or contents of the PEM file containing the public key (RSA and ECDSA).
    /// </summary>
    public string? VerificationKey { get; init; }

    /// <summary>
    /// Contents of the PEM file containing the public key (RSA and ECDSA).
    /// </summary>
    /// <remarks>
    /// Ignored if <see cref="VerificationKey"/> specified.
    /// </remarks>
    public string? VerificationKeyPath { get; init; }

    /// <summary>
    /// Name assigned to the verification key used for bundle verification (default "default").
    /// </summary>
    public string VerificationKeyId { get; init; } = "default";

    /// <summary>
    /// Bundle signing verification logic implementation.
    /// </summary>
    internal IBundleSignatureValidator Validator { get; init; } = new DefaultBundleSignatureValidator();
}