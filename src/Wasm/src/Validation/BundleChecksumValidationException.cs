using System.Diagnostics.CodeAnalysis;

namespace OpaDotNet.Wasm.Validation;

/// <summary>
/// The exception that is thrown if hash of file in the bundle mismatches hash in the signature.
/// </summary>
/// <param name="fileName">Name of the file with mismatched hash.</param>
/// <param name="alg">Hashing algorithm.</param>
/// <param name="expected">Expected hash.</param>
/// <param name="actual">Actual hash.</param>
[ExcludeFromCodeCoverage]
public class BundleChecksumValidationException(string fileName, string? alg, string? expected, string actual)
    : BundleSignatureValidationException($"Checksum hash mismatch for {fileName}")
{
    /// <summary>
    /// Name of the file with mismatched hash.
    /// </summary>
    public string FileName => fileName;

    /// <summary>
    /// Hashing algorithm.
    /// </summary>
    public string? Alg => alg;

    /// <summary>
    /// Expected hash.
    /// </summary>
    public string? Expected => expected;

    /// <summary>
    /// Actual hash.
    /// </summary>
    public string Actual => actual;
}