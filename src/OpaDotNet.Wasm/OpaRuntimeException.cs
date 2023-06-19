namespace OpaDotNet.Wasm;

public class OpaRuntimeException : Exception
{
    public OpaRuntimeException(string? message) : base(message)
    {
    }

    public OpaRuntimeException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}