namespace OpaDotNet.Wasm;

/// <summary>
/// The exception that is thrown when an error occurs while evaluating OPA policy.
/// </summary>
public class OpaEvaluationException : OpaRuntimeException
{
    /// <inheritdoc />
    public OpaEvaluationException(string? message) : base(message)
    {
    }

    /// <inheritdoc />
    public OpaEvaluationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when we need to abort rule evaluation and force it to return default result.
/// </summary>
[ExcludeFromCodeCoverage]
internal class OpaEvaluationAbortedException(string? message) : OpaEvaluationException(message);