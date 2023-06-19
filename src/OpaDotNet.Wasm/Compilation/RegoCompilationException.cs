namespace OpaDotNet.Wasm.Compilation;

public class RegoCompilationException : Exception
{
    public string SourceFile { get; private set; }

    public RegoCompilationException(string sourceFile, string? message) : base(message)
    {
        SourceFile = sourceFile;
    }

    public RegoCompilationException(string sourceFile, string? message, Exception? innerException) : base(message, innerException)
    {
        SourceFile = sourceFile;
    }
}