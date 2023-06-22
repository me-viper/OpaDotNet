using JetBrains.Annotations;

namespace OpaDotNet.Wasm;

[PublicAPI]
public class OpaRuntimeException : Exception
{
    public OpaRuntimeException(string? message) : base(message)
    {
    }

    public OpaRuntimeException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}