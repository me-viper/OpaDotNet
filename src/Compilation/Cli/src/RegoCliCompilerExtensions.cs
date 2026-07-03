using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Compilation.Cli;

/// <summary>
/// Compilation extensions.
/// </summary>
[PublicAPI]
public static class RegoCliCompilerExtensions
{
    /// <summary>
    /// Check Rego sources for parse and compilation errors.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="path">Bundle directory or bundle archive path.</param>
    /// <param name="parameters">Compiler parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of parsing and compiling the source file.</returns>
    public static Task<CheckResult> CheckBundleAsync(
        this RegoCliCompiler compiler,
        string path,
        CheckParameters parameters,
        CancellationToken cancellationToken)
    {
        return compiler.CheckAsync(
            path,
            parameters.IsBundle ? parameters : parameters with { IsBundle = true },
            cancellationToken
            );
    }

    /// <summary>
    /// Check Rego sources for parse and compilation errors.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="path">Bundle directory or bundle archive path.</param>
    /// <param name="parameters">Compiler parameters.</param>
    /// <returns>Result of parsing and compiling the source file.</returns>
    public static Task<CheckResult> CheckBundleAsync(
        this RegoCliCompiler compiler,
        string path,
        CheckParameters parameters)
        => compiler.CheckBundleAsync(path, parameters, CancellationToken.None);

    /// <summary>
    /// Check Rego sources for parse and compilation errors.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="stream">Rego bundle stream.</param>
    /// <param name="parameters">Compiler parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of parsing and compiling the source file.</returns>
    public static Task<CheckResult> CheckBundleAsync(
        this RegoCliCompiler compiler,
        Stream stream,
        CheckParameters parameters,
        CancellationToken cancellationToken)
    {
        return compiler.CheckAsync(stream, parameters, cancellationToken);
    }

    /// <summary>
    /// Check Rego sources for parse and compilation errors.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="stream">Rego bundle stream.</param>
    /// <param name="parameters">Compiler parameters.</param>
    /// <returns>Result of parsing and compiling the source file.</returns>
    public static Task<CheckResult> CheckBundleAsync(
        this RegoCliCompiler compiler,
        Stream stream,
        CheckParameters parameters)
        => compiler.CheckBundleAsync(stream, parameters, CancellationToken.None);

    /// <summary>
    /// Check Rego sources for parse and compilation errors.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="sourceFilePath">Source file path.</param>
    /// <param name="parameters">Compiler parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of parsing and compiling the source file.</returns>
    public static Task<CheckResult> CheckFileAsync(
        this RegoCliCompiler compiler,
        string sourceFilePath,
        CheckParameters parameters,
        CancellationToken cancellationToken)
    {
        return compiler.CheckAsync(sourceFilePath, parameters, cancellationToken);
    }

    /// <summary>
    /// Check Rego sources for parse and compilation errors.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="sourceFilePath">Source file path.</param>
    /// <param name="parameters">Compiler parameters.</param>
    /// <returns>Result of parsing and compiling the source file.</returns>
    public static Task<CheckResult> CheckFileAsync(
        this RegoCliCompiler compiler,
        string sourceFilePath,
        CheckParameters parameters)
        => compiler.CheckFileAsync(sourceFilePath, parameters, CancellationToken.None);

    /// <summary>
    /// Check Rego sources for parse and compilation errors.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="source">Policy source code.</param>
    /// <param name="parameters">Compiler parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of parsing and compiling the source file.</returns>
    public static async Task<CheckResult> CheckSourceAsync(
        this RegoCliCompiler compiler,
        string source,
        CheckParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(source);
        ArgumentNullException.ThrowIfNull(parameters);

        using var bundle = new MemoryStream();
        var bw = new BundleWriter(bundle);

        await using (bw.ConfigureAwait(false))
        {
            bw.WriteEntry(source, "policy.rego");
        }

        bundle.Seek(0, SeekOrigin.Begin);

        return await compiler.CheckAsync(
            bundle,
            parameters.IsBundle ? parameters : parameters with { IsBundle = true },
            cancellationToken
            ).ConfigureAwait(false);
    }

    /// <summary>
    /// Check Rego sources for parse and compilation errors.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="source">Policy source code.</param>
    /// <param name="parameters">Compiler parameters.</param>
    /// <returns>Result of parsing and compiling the source file.</returns>
    public static Task<CheckResult> CheckSourceAsync(
        this RegoCliCompiler compiler,
        string source,
        CheckParameters parameters)
        => compiler.CheckSourceAsync(source, parameters, CancellationToken.None);
}