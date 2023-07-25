using System.Diagnostics.CodeAnalysis;

namespace OpaDotNet.Wasm;

public class OpaEvaluationException : OpaRuntimeException
{
    public OpaEvaluationException(string? message) : base(message)
    {
    }

    public OpaEvaluationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when we need to abort rule evaluation and force it to return default result.
/// </summary>
[ExcludeFromCodeCoverage]
internal class OpaEvaluationAbortedException : OpaEvaluationException
{
    public OpaEvaluationAbortedException(string? message) : base(message)
    {
    }
}