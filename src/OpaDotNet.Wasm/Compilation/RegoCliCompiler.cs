using System.Diagnostics.CodeAnalysis;
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

    private string CliPath => string.IsNullOrWhiteSpace(_options.Value.OpaToolPath)
        ? "opa"
        : Path.Combine(_options.Value.OpaToolPath, "opa");

    /// <inheritdoc />
    public async Task<Stream> CompileBundle(
        string bundlePath,
        IEnumerable<string>? entrypoints = null,
        string? capabilitiesFilePath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(bundlePath);

        using var scope = _logger.BeginScope("Bundle {Path}", bundlePath);

        var cli = await OpaCliWrapper.Create(CliPath, _logger, cancellationToken).ConfigureAwait(false);

        var bundleDirectory = new DirectoryInfo(bundlePath);

        var outDir = new DirectoryInfo(_options.Value.OutputPath ?? bundleDirectory.FullName);
        var outputPath = outDir.FullName;
        var outputFileName = Path.Combine(outputPath, $"{Guid.NewGuid()}.tar.gz");

        string? capabilitiesFile = null;
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
                    cli,
                    outputPath,
                    fi,
                    _options.Value.CapabilitiesVersion,
                    cancellationToken
                    ).ConfigureAwait(false);
            }

            capabilitiesFile = capsFile?.FullName ?? fi.FullName;
        }

        var args = new OpaCliBuildArgs
        {
            IsBundle = true,
            SourcePath = bundleDirectory.FullName,
            OutputFile = outputFileName,
            Entrypoints = entrypoints?.ToHashSet(),
            ExtraArguments = _options.Value.ExtraArguments,
            CapabilitiesFile = capabilitiesFile,
        };

        try
        {
            return await Build(cli, args, cancellationToken).ConfigureAwait(false);
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

        using var scope = _logger.BeginScope("File {Path}", sourceFilePath);

        var cli = await OpaCliWrapper.Create(CliPath, _logger, cancellationToken).ConfigureAwait(false);

        var sourceFile = new FileInfo(sourceFilePath);

        if (!sourceFile.Exists)
            throw new RegoCompilationException(sourceFilePath, $"Source file {sourceFilePath} not found");

        var outDir = new DirectoryInfo(_options.Value.OutputPath ?? sourceFile.Directory!.FullName);
        var outputPath = outDir.FullName;
        var outputFileName = Path.Combine(outputPath, $"{Guid.NewGuid()}.tar.gz");

        var args = new OpaCliBuildArgs
        {
            SourcePath = sourceFile.FullName,
            OutputFile = outputFileName,
            Entrypoints = entrypoints?.ToHashSet(),
            ExtraArguments = _options.Value.ExtraArguments,
        };

        return await Build(cli, args, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<FileInfo> MergeCapabilities(
        OpaCliWrapper cli,
        string outputPath,
        FileInfo file,
        string version,
        CancellationToken cancellationToken)
    {
        var capsFileName = Path.Combine(outputPath, $"{Guid.NewGuid()}.json");
        var result = new FileInfo(capsFileName);

        await cli.Capabilities(result.FullName, version, cancellationToken).ConfigureAwait(false);

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
            await capsFs.FlushAsync(cancellationToken).ConfigureAwait(false);

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

    private async Task<Stream> Build(
        OpaCliWrapper cli,
        OpaCliBuildArgs args,
        CancellationToken cancellationToken)
    {
        await cli.Build(args, cancellationToken).ConfigureAwait(false);

        if (!File.Exists(args.OutputFile))
        {
            throw new RegoCompilationException(
                args.SourcePath,
                $"Failed to locate expected output file {args.OutputFile}"
                );
        }

        _logger.LogInformation("Compilation succeeded");

        return _options.Value.PreserveBuildArtifacts
            ? new FileStream(args.OutputFile, FileMode.Open)
            : new DeleteOnCloseFileStream(args.OutputFile, FileMode.Open);
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