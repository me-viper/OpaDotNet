namespace OpaDotNet.Compilation.Abstractions;

/// <summary>
/// The exception that is thrown when OPA policy compilation fails.
/// </summary>
[PublicAPI]
public class RegoCompilationException : Exception
{
    /// <summary>
    /// Source file that caused the current exception.
    /// </summary>
    public string? SourceFile { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegoCompilationException"/>.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public RegoCompilationException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegoCompilationException"/>.
    /// </summary>
    /// <param name="sourceFile">Source file that caused the current exception.</param>
    /// <param name="message">The message that describes the error.</param>
    public RegoCompilationException(string sourceFile, string? message) : base(message)
    {
        SourceFile = sourceFile;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegoCompilationException"/>.
    /// </summary>
    /// <param name="sourceFile">Source file that caused the current exception.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public RegoCompilationException(string sourceFile, string? message, Exception? innerException) : base(message, innerException)
    {
        SourceFile = sourceFile;
    }
}