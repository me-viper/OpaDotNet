using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using Microsoft.Extensions.Options;

namespace OpaDotNet.Wasm.Compilation;

/// <summary>
/// Compiles OPA bundle with opa cli tool.
/// </summary>
public class RegoCliCompiler : IRegoCompiler
{
    private readonly ILogger _logger;

    private readonly IOptions<RegoCliCompilerOptions> _options;

    /// <summary>
    /// Creates new instance of <see cref="RegoCliCompiler"/> class.
    /// </summary>
    /// <param name="options">Compilation options</param>
    /// <param name="logger">Logger instance</param>
    public RegoCliCompiler(
        IOptions<RegoCliCompilerOptions> options,
        ILogger<RegoCliCompiler>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _logger = logger ?? NullLogger<RegoCliCompiler>.Instance;
    }

    /// <inheritdoc />
    public async Task<Stream> CompileBundle(
        string bundlePath,
        IEnumerable<string>? entrypoints = null,
        string? capabilitiesFilePath = null,
        CancellationToken cancellationToken = default)
    {
        var bundleDirectory = new DirectoryInfo(bundlePath);

        var entrypointArg = string.Empty;

        if (entrypoints != null)
            entrypointArg = string.Join(" ", entrypoints.Select(p => $"-e {p}"));

        var capabilitiesArg = string.Empty;

        if (!string.IsNullOrWhiteSpace(capabilitiesFilePath))
        {
            var fi = new FileInfo(capabilitiesFilePath);

            if (!fi.Exists)
            {
                throw new RegoCompilationException(
                    bundlePath,
                    $"Capabilities file {fi.FullName} was not found"
                    );
            }

            capabilitiesArg = $"--capabilities {fi.FullName}";
        }

        using var scope = _logger.BeginScope("Bundle {Path}", bundlePath);

        var outputPath = _options.Value.OutputPath ?? bundleDirectory.FullName;
        var outputFileName = Path.Combine(outputPath, $"{Guid.NewGuid()}.tar.gz");

        var args = $"build -b -t wasm {entrypointArg} {capabilitiesArg} -o {outputFileName} " +
            $"{_options.Value.ExtraArguments} {bundleDirectory.FullName}";

        return await Run(bundleDirectory.FullName, bundleDirectory.FullName, args, outputFileName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Stream> CompileFile(
        string sourceFilePath,
        IEnumerable<string>? entrypoints = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceFilePath);

        var fi = new FileInfo(sourceFilePath);

        if (!fi.Exists)
            throw new RegoCompilationException(sourceFilePath, $"Source file {sourceFilePath} not found");

        using var scope = _logger.BeginScope("File {Path}", sourceFilePath);

        var outputPath = _options.Value.OutputPath ?? fi.Directory!.FullName;
        var outputFileName = Path.Combine(outputPath, $"{Guid.NewGuid()}.tar.gz");

        var entrypointArg = string.Empty;

        if (entrypoints != null)
            entrypointArg = string.Join(" ", entrypoints.Select(p => $"-e {p}"));

        // opa build -t wasm -e example/hello .\simple.rego
        var args = $"build -t wasm {entrypointArg} -o {outputFileName} {_options.Value.ExtraArguments} {fi.FullName}";

        return await Run(fi.Directory!.FullName, fi.FullName, args, outputFileName, cancellationToken);
    }

    private async Task<Stream> Run(
        string basePath,
        string sourcePath,
        string args,
        string outputFileName,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Running opa {Args}", args);

        var fileName = string.IsNullOrWhiteSpace(_options.Value.OpaToolPath)
            ? "opa"
            : Path.Combine(_options.Value.OpaToolPath, "opa");

        var versionProcessInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = "version",
            WorkingDirectory = basePath,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        using var versionProcess = Process.Start(versionProcessInfo);

        if (versionProcess == null)
        {
            throw new RegoCompilationException(
                sourcePath,
                "Failed to start compilation process"
                );
        }

        _ = await RunProcess(versionProcess, sourcePath, cancellationToken);

        var compilationProcess = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            WorkingDirectory = basePath,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        _logger.LogInformation("Starting compilation [opa {Cli}]", compilationProcess.Arguments);

        using var process = Process.Start(compilationProcess);

        if (process == null)
        {
            throw new RegoCompilationException(
                sourcePath,
                "Failed to start compilation process"
                );
        }

        var errors = await RunProcess(process, sourcePath, cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new RegoCompilationException(
                sourcePath,
                $"Return code {process.ExitCode} didn't indicate success.\nErrors: {errors}"
                );
        }

        if (!File.Exists(outputFileName))
        {
            throw new RegoCompilationException(
                sourcePath,
                $"Failed to locate expected output file {outputFileName}"
                );
        }

        _logger.LogInformation("Compilation succeeded");
        return new DeleteOnCloseFileStream(outputFileName, FileMode.Open);
    }

    private async Task<StringBuilder> RunProcess(
        Process process,
        string sourcePath,
        CancellationToken cancellationToken)
    {
        var errors = new StringBuilder();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.OutputDataReceived += (_, ea) =>
        {
            if (string.IsNullOrWhiteSpace(ea.Data))
                return;

            _logger.LogInformation("{CompilationProgressLog}", ea.Data);
        };

        process.ErrorDataReceived += (_, ea) =>
        {
            if (string.IsNullOrWhiteSpace(ea.Data))
                return;

            errors.AppendLine(ea.Data);
            _logger.LogError("{CompilationErrorLog}", ea.Data ?? string.Empty);
        };

        using var timeoutCancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var ct = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCancellationToken.Token
            );

        try
        {
            await process.WaitForExitAsync(ct.Token);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Failed to complete compilation within specified timeout");
            process.Kill(true);

            throw new RegoCompilationException(
                sourcePath,
                "Failed to complete compilation within specified timeout",
                ex
                );
        }

        return errors;
    }
    
    [ExcludeFromCodeCoverage]
    private class DeleteOnCloseFileStream : FileStream
    {
        private readonly string _path; 
        
        public DeleteOnCloseFileStream(string path, FileMode mode) : base(path, mode)
        {
            _path = path;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            
            if (disposing)
            {
                DeleteFile();
            }
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            DeleteFile();
        }
        
        private void DeleteFile()
        {
            try
            {
                File.Delete(_path);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
            }
        }
    }
}