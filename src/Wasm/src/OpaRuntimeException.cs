namespace OpaDotNet.Wasm;

/// <summary>
/// The exception that is thrown when OPA runtime encounters an error.
/// </summary>
[PublicAPI]
public class OpaRuntimeException : Exception
{
    /// <inheritdoc />
    public OpaRuntimeException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public OpaRuntimeException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}