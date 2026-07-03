namespace OpaDotNet.Compilation.Cli;

/// <summary>
/// Check results.
/// </summary>
public class CheckResult
{
    /// <summary>
    /// Check succeeded in parsing and compiling the source file(s).
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Check exit code.
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Check output.
    /// </summary>
    public string? Output { get; set; }
}