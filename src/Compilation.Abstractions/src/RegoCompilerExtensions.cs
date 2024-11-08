namespace OpaDotNet.Compilation.Abstractions;

/// <summary>
/// Compilation extensions.
/// </summary>
[PublicAPI]
public static class RegoCompilerExtensions
{
    /// <summary>
    /// Compiles OPA bundle from bundle directory.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="bundlePath">Bundle directory or bundle archive path.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="capabilitiesFilePath">
    /// Capabilities file that defines the built-in functions and other language features that policies may depend on.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    [Obsolete("Use IRegoCompiler.CompileBundleAsync instead")]
    public static async Task<Stream> CompileBundle(
        this IRegoCompiler compiler,
        string bundlePath,
        IEnumerable<string>? entrypoints = null,
        string? capabilitiesFilePath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(bundlePath);

        var opts = new CompilationParameters
        {
            Entrypoints = entrypoints?.ToList(),
            CapabilitiesFilePath = capabilitiesFilePath,
            IsBundle = true,
        };

        return await compiler.CompileBundleAsync(bundlePath, opts, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Compiles OPA bundle from rego policy source file.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="sourceFilePath">Source file path.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    [Obsolete("Use IRegoCompiler.CompileFileAsync instead")]
    public static Task<Stream> CompileFile(
        this IRegoCompiler compiler,
        string sourceFilePath,
        IEnumerable<string>? entrypoints = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceFilePath);

        var opts = new CompilationParameters { Entrypoints = entrypoints?.ToList() };
        return compiler.Compile(sourceFilePath, opts, cancellationToken);
    }

    /// <summary>
    /// Compiles OPA bundle from rego bundle stream.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="bundle">Rego bundle stream.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="capabilitiesJson">
    /// Capabilities json that defines the built-in functions and other language features that policies may depend on.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    [Obsolete("Use IRegoCompiler.CompileBundleAsync instead")]
    public static Task<Stream> CompileStream(
        this IRegoCompiler compiler,
        Stream bundle,
        IEnumerable<string>? entrypoints = null,
        Stream? capabilitiesJson = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        var capsMem = Memory<byte>.Empty;

        if (capabilitiesJson != null)
        {
            capsMem = new byte[(int)capabilitiesJson.Length];
            _ = capabilitiesJson.Read(capsMem.Span);
        }

        var opts = new CompilationParameters
        {
            Entrypoints = entrypoints?.ToList(),
            CapabilitiesBytes = capsMem,
            IsBundle = true,
        };

        return compiler.CompileBundleAsync(bundle, opts, cancellationToken);
    }

    /// <summary>
    /// Compiles OPA bundle from rego policy source code.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="source">Source file path.</param>
    /// <param name="entrypoints">Which documents (entrypoints) will be queried when asking for policy decisions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    [Obsolete("Use IRegoCompiler.CompileSourceAsync instead")]
    public static Task<Stream> CompileSource(
        this IRegoCompiler compiler,
        string source,
        IEnumerable<string>? entrypoints = null,
        CancellationToken cancellationToken = default)
    {
        var opts = new CompilationParameters
        {
            Entrypoints = entrypoints?.ToList(),
            IsBundle = true,
        };

        return compiler.CompileSourceAsync(source, opts, cancellationToken);
    }

    /// <summary>
    /// Compiles OPA bundle from rego bundle stream.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="sourceFilePath">Source file path.</param>
    /// <param name="parameters">Compiler parameters.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    public static Task<Stream> CompileFileAsync(
        this IRegoCompiler compiler,
        string sourceFilePath,
        CompilationParameters parameters) => CompileFileAsync(compiler, sourceFilePath, parameters, CancellationToken.None);

    /// <summary>
    /// Compiles OPA bundle from rego bundle stream.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="sourceFilePath">Source file path.</param>
    /// <param name="parameters">Compiler parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Compiled OPA bundle stream.</returns>
    public static async Task<Stream> CompileFileAsync(
        this IRegoCompiler compiler,
        string sourceFilePath,
        CompilationParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceFilePath);
        ArgumentNullException.ThrowIfNull(parameters);

        return await compiler.Compile(
            sourceFilePath,
            parameters,
            cancellationToken
            ).ConfigureAwait(false);
    }

    /// <summary>
    /// Compiles OPA bundle from rego policy source code.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="source">Source file path.</param>
    /// <param name="parameters">Compiler parameters.</param>
    /// <remarks>This method always compiles contents as bundle.</remarks>
    /// <returns>Compiled OPA bundle stream.</returns>
    public static Task<Stream> CompileSourceAsync(
        this IRegoCompiler compiler,
        string source,
        CompilationParameters parameters) => CompileSourceAsync(compiler, source, parameters, CancellationToken.None);

    /// <summary>
    /// Compiles OPA bundle from rego policy source code.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="source">Source file path.</param>
    /// <param name="parameters">Compiler parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>This method always compiles contents as bundle.</remarks>
    /// <returns>Compiled OPA bundle stream.</returns>
    public static async Task<Stream> CompileSourceAsync(
        this IRegoCompiler compiler,
        string source,
        CompilationParameters parameters,
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

        return await compiler.Compile(
            bundle,
            parameters.IsBundle ? parameters : parameters with { IsBundle = true },
            cancellationToken
            ).ConfigureAwait(false);
    }

    /// <summary>
    /// Compiles OPA bundle from path.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="path">Bundle directory or bundle archive path.</param>
    /// <param name="parameters">Compiler parameters.</param>
    /// <remarks>This method always compiles contents as bundle.</remarks>
    /// <returns>Compiled OPA bundle stream.</returns>
    public static Task<Stream> CompileBundleAsync(
        this IRegoCompiler compiler,
        string path,
        CompilationParameters parameters) => CompileBundleAsync(compiler, path, parameters, CancellationToken.None);

    /// <summary>
    /// Compiles OPA bundle from rego bundle stream.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="path">Bundle directory or bundle archive path.</param>
    /// <param name="parameters">Compiler parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>This method always compiles contents as bundle.</remarks>
    /// <returns>Compiled OPA bundle stream.</returns>
    public static Task<Stream> CompileBundleAsync(
        this IRegoCompiler compiler,
        string path,
        CompilationParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(parameters);

        return compiler.Compile(
            path,
            parameters.IsBundle ? parameters : parameters with { IsBundle = true },
            cancellationToken
            );
    }

    /// <summary>
    /// Compiles OPA bundle from rego bundle stream.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="bundle">Rego bundle stream.</param>
    /// <param name="parameters">Compiler parameters.</param>
    /// <remarks>This method always compiles contents as bundle.</remarks>
    /// <returns>Compiled OPA bundle stream.</returns>
    public static Task<Stream> CompileBundleAsync(
        this IRegoCompiler compiler,
        Stream bundle,
        CompilationParameters parameters) => CompileBundleAsync(compiler, bundle, parameters, CancellationToken.None);

    /// <summary>
    /// Compiles OPA bundle from rego bundle stream.
    /// </summary>
    /// <param name="compiler">Compiler instance.</param>
    /// <param name="bundle">Rego bundle stream.</param>
    /// <param name="parameters">Compiler parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>This method always compiles contents as bundle.</remarks>
    /// <returns>Compiled OPA bundle stream.</returns>
    public static Task<Stream> CompileBundleAsync(
        this IRegoCompiler compiler,
        Stream bundle,
        CompilationParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        ArgumentNullException.ThrowIfNull(parameters);

        return compiler.Compile(
            bundle,
            parameters.IsBundle ? parameters : parameters with { IsBundle = true },
            cancellationToken
            );
    }
}