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