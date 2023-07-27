using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Nodes;

using Microsoft.Extensions.Options;

namespace OpaDotNet.Wasm.Compilation;

/// <summary>
/// Compiles OPA bundle with opa cli tool.
/// </summary>
public class RegoCliCompiler : IRegoCompiler
{
    private static IOptions<RegoCliCompilerOptions> Default { get; } = new OptionsWrapper<RegoCliCompilerOptions>(new());

    private readonly ILogger _logger;

    private readonly IOptions<RegoCliCompilerOptions> _options;

    /// <summary>
    /// Creates new instance of <see cref="RegoCliCompiler"/> class.
    /// </summary>
    /// <param name="options">Compilation options</param>
    /// <param name="logger">Logger instance</param>
    public RegoCliCompiler(
        IOptions<RegoCliCompilerOptions>? options = null,
        ILogger<RegoCliCompiler>? logger = null)
    {
        _options = options ?? Default;
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

        var outDir = new DirectoryInfo(_options.Value.OutputPath ?? bundleDirectory.FullName);
        var outputPath = outDir.FullName;

        var capabilitiesArg = string.Empty;
        FileInfo? capsFile = null;

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

            if (!string.IsNullOrWhiteSpace(_options.Value.CapabilitiesVersion))
            {
                capsFile = await MergeCapabilities(
                    outputPath,
                    fi,
                    _options.Value.CapabilitiesVersion,
                    cancellationToken
                    ).ConfigureAwait(false);
            }

            capabilitiesArg = $"--capabilities {capsFile?.FullName ?? fi.FullName}";
        }

        using var scope = _logger.BeginScope("Bundle {Path}", bundlePath);

        var outputFileName = Path.Combine(outputPath, $"{Guid.NewGuid()}.tar.gz");

        var args = $"build -b -t wasm {entrypointArg} {capabilitiesArg} -o {outputFileName} " +
            $"{_options.Value.ExtraArguments} {bundleDirectory.FullName}";

        try
        {
            return await Run(bundleDirectory.FullName, args, outputFileName, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (!_options.Value.PreserveBuildArtifacts)
                capsFile?.Delete();
        }
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

        var outDir = new DirectoryInfo(_options.Value.OutputPath ?? fi.Directory!.FullName);
        var outputPath = outDir.FullName;
        var outputFileName = Path.Combine(outputPath, $"{Guid.NewGuid()}.tar.gz");

        var entrypointArg = string.Empty;

        if (entrypoints != null)
            entrypointArg = string.Join(" ", entrypoints.Select(p => $"-e {p}"));

        var args = $"build -t wasm {entrypointArg} -o {outputFileName} {_options.Value.ExtraArguments} {fi.FullName}";

        return await Run(fi.FullName, args, outputFileName, cancellationToken).ConfigureAwait(false);
    }

    private async Task<FileInfo> MergeCapabilities(
        string outputPath,
        FileInfo file,
        string version,
        CancellationToken cancellationToken)
    {
        var fileName = string.IsNullOrWhiteSpace(_options.Value.OpaToolPath)
            ? "opa"
            : Path.Combine(_options.Value.OpaToolPath, "opa");

        var capsFileName = Path.Combine(outputPath, $"{Guid.NewGuid()}.json");
        var result = new FileInfo(capsFileName);

        var sw = new StreamWriter(result.FullName);
        await using var _ = sw.ConfigureAwait(false);

        var capsProcessInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = $"capabilities --version {version}",
            WorkingDirectory = AppContext.BaseDirectory,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        };

        using var capsProcess = Process.Start(capsProcessInfo);

        if (capsProcess == null)
        {
            throw new RegoCompilationException(
                outputPath,
                "Failed to start compilation process"
                );
        }

        _logger.LogInformation("Writing {Version} capabilities to {File}", version, result.FullName);

        capsProcess.OutputDataReceived += (_, args) => sw.WriteLine(args.Data);
        capsProcess.BeginOutputReadLine();

        using var timeoutCancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var ct = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCancellationToken.Token
            );

        try
        {
            await capsProcess.WaitForExitAsync(ct.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Failed to complete compilation within specified timeout");
            capsProcess.Kill(true);

            throw new RegoCompilationException(
                outputPath,
                "Failed to complete compilation within specified timeout",
                ex
                );
        }

        if (capsProcess.ExitCode != 0)
        {
            throw new RegoCompilationException(
                outputPath,
                $"Return code {capsProcess.ExitCode} didn't indicate success."
                );
        }

        await sw.FlushAsync().ConfigureAwait(false);
        await sw.DisposeAsync().ConfigureAwait(false);

        if (!result.Exists)
            throw new RegoCompilationException(capsFileName, "Failed to locate capabilities file");

        try
        {
            var capsFs = result.Open(FileMode.Open, FileAccess.ReadWrite);
            await using var __ = capsFs.ConfigureAwait(false);
            var capsDoc = JsonNode.Parse(capsFs);

            if (capsDoc == null)
                throw new RegoCompilationException(result.FullName, "Failed to parse capabilities file");

            var capsBins = capsDoc.Root["builtins"]?.AsArray();

            if (capsBins == null)
                throw new RegoCompilationException(result.FullName, "Failed to parse capabilities file");

            var exCapsFs = file.OpenRead();
            await using var ___ = exCapsFs.ConfigureAwait(false);

            var exCapsDoc = await JsonDocument.ParseAsync(exCapsFs, default, cancellationToken).ConfigureAwait(false);
            var exCapsBins = exCapsDoc.RootElement.GetProperty("builtins");

            foreach (var bin in exCapsBins.EnumerateArray())
                capsBins.Add(bin);

            capsFs.SetLength(0);
            await capsFs.FlushAsync(ct.Token).ConfigureAwait(false);

            var writer = new Utf8JsonWriter(capsFs);
            await using var ____ = writer.ConfigureAwait(false);

            capsDoc.WriteTo(writer);
        }
        catch (Exception ex)
        {
            throw new RegoCompilationException(
                outputPath,
                $"Failed to parse capabilities file {file.FullName}",
                ex
                );
        }

        return result;
    }

    private async Task<Stream> Run(
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
            WorkingDirectory = AppContext.BaseDirectory,
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

        _ = await RunProcess(versionProcess, sourcePath, cancellationToken).ConfigureAwait(false);

        var compilationProcess = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            WorkingDirectory = AppContext.BaseDirectory,
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

        var errors = await RunProcess(process, sourcePath, cancellationToken).ConfigureAwait(false);

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

        return _options.Value.PreserveBuildArtifacts
            ? new FileStream(outputFileName, FileMode.Open)
            : new DeleteOnCloseFileStream(outputFileName, FileMode.Open);
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
            await process.WaitForExitAsync(ct.Token).ConfigureAwait(false);
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
            await base.DisposeAsync().ConfigureAwait(false);
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