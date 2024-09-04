using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Compilation.Interop;

/// <summary>
/// Compiles OPA bundle with OPA SDK interop wrapper.
/// </summary>
public class RegoInteropCompiler : IRegoCompiler
{
    private readonly ILogger _logger;

    /// <summary>
    /// Creates new instance of <see cref="RegoInteropCompiler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public RegoInteropCompiler(ILogger<RegoInteropCompiler>? logger = null)
    {
        _logger = logger ?? NullLogger<RegoInteropCompiler>.Instance;
    }

    private static string NormalizePath(string path) => path.Replace("\\", "/");

    /// <inheritdoc />
    public Task<RegoCompilerVersion> Version(CancellationToken cancellationToken = default)
    {
        var vp = nint.Zero;

        try
        {
            Interop.OpaGetVersion(out vp);

            if (vp == nint.Zero)
                throw new RegoCompilationException("Failed to get version");

            var v = Marshal.PtrToStructure<Interop.OpaVersion>(vp);

            var result = new RegoCompilerVersion
            {
                Version = v.LibVersion,
                Commit = v.Commit,
                Platform = v.Platform,
                GoVersion = v.GoVersion,
            };

            return Task.FromResult(result);
        }
        finally
        {
            if (vp != nint.Zero)
                Interop.OpaFreeVersion(vp);
        }
    }

    /// <inheritdoc />
    public async Task<Stream> Compile(
        string path,
        CompilationParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(parameters);

        if (path.StartsWith("./") || path.StartsWith(".\\"))
            path = path[2..];

        Stream? caps = null;

        try
        {
            var result = Interop.Compile(
                NormalizePath(path),
                parameters,
                false,
                _logger
                );

            return result;
        }
        finally
        {
            if (caps != null)
                await caps.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<Stream> Compile(
        Stream stream,
        CompilationParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(parameters);

        Stream? bundle = null;

        try
        {
            if (!parameters.IsBundle)
            {
                bundle = new MemoryStream();
                var bw = new BundleWriter(bundle);

                await using (bw.ConfigureAwait(false))
                    bw.WriteEntry(stream, "policy.rego");

                bundle.Seek(0, SeekOrigin.Begin);
            }

            var result = Interop.Compile(
                bundle ?? stream,
                parameters,
                true,
                _logger
                );

            return result;
        }
        finally
        {
            if (bundle != null)
                await bundle.DisposeAsync().ConfigureAwait(false);
        }
    }
}