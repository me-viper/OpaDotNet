using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Compilation.Cli;

/// <summary>
/// Compiles OPA bundle with opa cli tool.
/// </summary>
[PublicAPI]
public class RegoCliCompiler : IRegoCompiler
{
    private readonly ILogger _logger;

    private readonly RegoCliCompilerOptions _options;

    /// <summary>
    /// Creates new instance of <see cref="RegoCliCompiler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="options">Compilation options</param>
    public RegoCliCompiler(ILogger<RegoCliCompiler>? logger = null, RegoCliCompilerOptions? options = null)
    {
        _options = options ?? new();
        _logger = logger ?? NullLogger<RegoCliCompiler>.Instance;
    }

    private string CliPath => string.IsNullOrWhiteSpace(_options.OpaToolPath)
        ? "opa"
        : _options.OpaToolPath;

    private static string NormalizePath(string path) => path.Replace("\\", "/");

    /// <inheritdoc />
    public async Task<RegoCompilerVersion> Version(CancellationToken cancellationToken = default)
    {
        var cli = await OpaCliWrapper.Create(CliPath, _logger, cancellationToken).ConfigureAwait(false);
        return cli.VersionInfo;
    }

    /// <inheritdoc />
    public async Task<Stream> Compile(
        string path,
        CompilationParameters parameters,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(parameters);

        path = NormalizePath(path);

        using var scope = _logger.BeginScope("Bundle {Path}", path);

        var cli = await OpaCliWrapper.Create(CliPath, _logger, cancellationToken).ConfigureAwait(false);

        var fullPath = Path.GetFullPath(path);

        // bundlePath can be directory or bundle archive.
        var bundleDirectory =
            File.GetAttributes(fullPath).HasFlag(FileAttributes.Directory)
                ? new DirectoryInfo(fullPath)
                : new FileInfo(fullPath).Directory!;

        var outDir = new DirectoryInfo(parameters.OutputPath ?? bundleDirectory.FullName);
        var outputPath = outDir.FullName;
        var outputFileName = Path.Combine(outputPath, $"{Guid.NewGuid()}.tar.gz");

        var capsFile = await WriteCapabilities(cli, parameters, outputPath, cancellationToken).ConfigureAwait(false);

        var sourcePath = fullPath;

        if (Path.IsPathRooted(path))
            sourcePath = Path.GetRelativePath(AppContext.BaseDirectory, path);

        var args = new OpaCliBuildArgs
        {
            IsBundle = parameters.IsBundle,
            SourcePath = sourcePath,
            OutputFile = outputFileName,
            Entrypoints = parameters.Entrypoints?.ToHashSet(),
            ExtraArguments = _options.ExtraArguments,
            CapabilitiesFile = capsFile?.FullName,
            CapabilitiesVersion = parameters.CapabilitiesVersion,
            PruneUnused = parameters.PruneUnused,
            Debug = parameters.Debug,
            Ignore = parameters.Ignore,
            Revision = parameters.Revision,
            RegoVersion = parameters.RegoVersion,
            FollowSymlinks = parameters.FollowSymlinks,
        };

        try
        {
            return await Build(cli, args, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            var doCleanup = capsFile != null && capsFile.Attributes.HasFlag(FileAttributes.Temporary);

            if (doCleanup && !_options.PreserveBuildArtifacts)
                capsFile?.Delete();
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

        var path = parameters.OutputPath ?? AppContext.BaseDirectory;
        var fileName = Guid.NewGuid();
        var sourceFile = new FileInfo(Path.Combine(path, $"{fileName}.tar.gz"));

        try
        {
            var fs = new FileStream(sourceFile.FullName, FileMode.CreateNew);
            await using var _ = fs.ConfigureAwait(false);

            await stream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
            await fs.FlushAsync(cancellationToken).ConfigureAwait(false);

            return await Compile(
                fs.Name,
                parameters,
                cancellationToken
                ).ConfigureAwait(false);
        }
        finally
        {
            if (!_options.PreserveBuildArtifacts)
                sourceFile.Delete();
        }
    }

    private async Task<FileInfo?> WriteCapabilities(
        OpaCliWrapper cli,
        CompilationParameters parameters,
        string outputPath,
        CancellationToken cancellationToken)
    {
        FileInfo? result = null;

        if (string.IsNullOrWhiteSpace(parameters.CapabilitiesFilePath) && parameters.CapabilitiesBytes.IsEmpty)
            return null;

        if (!string.IsNullOrWhiteSpace(parameters.CapabilitiesFilePath))
        {
            result = new FileInfo(parameters.CapabilitiesFilePath);

            if (!result.Exists)
                throw new RegoCompilationException($"Capabilities file {result.FullName} was not found");
        }

        if (!parameters.CapabilitiesBytes.IsEmpty)
        {
            var capsFileName = Path.Combine(outputPath, $"{Guid.NewGuid()}.json");
            result = new FileInfo(capsFileName);

            var fs = result.OpenWrite();
            await using var _ = fs.ConfigureAwait(false);

            await fs.WriteAsync(parameters.CapabilitiesBytes, cancellationToken).ConfigureAwait(false);
            await fs.FlushAsync(cancellationToken).ConfigureAwait(false);

            result.Attributes |= FileAttributes.Temporary;
        }

        if (result == null || string.IsNullOrWhiteSpace(parameters.CapabilitiesVersion))
            return result;

        var tmpCaps = result;

        try
        {
            var capsStream = result.OpenRead();
            await using var __ = capsStream.ConfigureAwait(false);

            result = await MergeCapabilities(
                cli,
                outputPath,
                capsStream,
                parameters.CapabilitiesVersion,
                cancellationToken
                ).ConfigureAwait(false);

            result.Attributes |= FileAttributes.Temporary;
            return result;
        }
        finally
        {
            if (tmpCaps.Attributes.HasFlag(FileAttributes.Temporary))
                tmpCaps.Delete();
        }
    }

    private static async Task<FileInfo> MergeCapabilities(
        OpaCliWrapper cli,
        string outputPath,
        Stream capabilities,
        string version,
        CancellationToken cancellationToken)
    {
        var capsFileName = Path.Combine(outputPath, $"{Guid.NewGuid()}.json");
        var defaultCaps = new FileInfo(capsFileName);

        await cli.Capabilities(defaultCaps.FullName, version, cancellationToken).ConfigureAwait(false);

        if (!defaultCaps.Exists)
            throw new RegoCompilationException(capsFileName, "Failed to locate capabilities file");

        try
        {
            var defaultCapsFs = defaultCaps.Open(FileMode.Open, FileAccess.ReadWrite);
            await using var _ = defaultCapsFs.ConfigureAwait(false);

            var capsFs = capabilities;
            await using var __ = capsFs.ConfigureAwait(false);

            var ms = BundleWriter.MergeCapabilities(defaultCapsFs, capsFs);
            await using var ___ = ms.ConfigureAwait(false);

            defaultCapsFs.SetLength(0);
            await defaultCapsFs.FlushAsync(cancellationToken).ConfigureAwait(false);

            await ms.CopyToAsync(defaultCapsFs, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new RegoCompilationException(
                outputPath,
                "Failed to parse capabilities",
                ex
                );
        }

        return defaultCaps;
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

        return _options.PreserveBuildArtifacts
            ? new FileStream(args.OutputFile, FileMode.Open, FileAccess.Read)
            : new DeleteOnCloseFileStream(args.OutputFile, FileMode.Open);
    }

    [ExcludeFromCodeCoverage]
    private class DeleteOnCloseFileStream(string path, FileMode mode) : FileStream(path, mode, FileAccess.Read)
    {
        private readonly string _path = path;

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