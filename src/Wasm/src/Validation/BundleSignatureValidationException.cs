using System.Diagnostics.CodeAnalysis;

namespace OpaDotNet.Wasm.Validation;

/// <summary>
/// The exception that is thrown when there is bundle signature validation error.
/// </summary>
[ExcludeFromCodeCoverage]
public class BundleSignatureValidationException : OpaRuntimeException
{
    /// <inheritdoc />
    public BundleSignatureValidationException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public BundleSignatureValidationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}