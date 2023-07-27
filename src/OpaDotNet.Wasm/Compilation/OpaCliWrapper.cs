using System.Diagnostics;
using System.Text;

namespace OpaDotNet.Wasm.Compilation;

internal class OpaCliWrapper
{
    private readonly string _opaCliPath;

    private readonly ILogger _logger;

    public OpaCliVersion VersionInfo { get; } = new();

    private OpaCliWrapper(string opaCliPath, ILogger? logger)
    {
        _opaCliPath = opaCliPath;
        _logger = logger ?? NullLogger.Instance;
    }

    private ProcessStartInfo CreateProcess(string command, string? args)
    {
        return new ProcessStartInfo
        {
            FileName = _opaCliPath,
            Arguments = $"{command} {args}",
            WorkingDirectory = AppContext.BaseDirectory,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
    }

    private async Task<int> Run(
        ProcessStartInfo psi,
        Action<string?> writeOutput,
        string? sourcePath = null,
        bool suppressLogging = false,
        CancellationToken cancellationToken = default)
    {
        using var process = Process.Start(psi);

        sourcePath ??= AppContext.BaseDirectory;

        if (process == null)
            throw new RegoCompilationException(sourcePath, "Failed to start compilation process");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.OutputDataReceived += (_, ea) =>
        {
            if (string.IsNullOrWhiteSpace(ea.Data))
                return;

            if (!suppressLogging)
                _logger.LogDebug("{Output}", ea.Data);

            writeOutput(ea.Data);
        };

        process.ErrorDataReceived += (_, ea) =>
        {
            if (string.IsNullOrWhiteSpace(ea.Data))
                return;

            _logger.LogError("{Error}", ea.Data);
        };

        _logger.LogDebug("Running [{Cli} {Args}]", psi.FileName, psi.Arguments);

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

        return process.ExitCode;
    }

    public async Task Build(OpaCliBuildArgs args, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Building");

        var sb = new StringBuilder();

        var code = await Run(
            CreateProcess("build", args.ToString()),
            p => sb.AppendLine(p),
            args.SourcePath,
            false,
            cancellationToken
            ).ConfigureAwait(false);

        if (code != 0)
        {
            throw new RegoCompilationException(
                args.SourcePath,
                $"Return code {code} didn't indicate success.\nOutput: {sb}"
                );
        }
    }

    public async Task Capabilities(string outputFileName, string version, CancellationToken cancellationToken)
    {
        var sw = new StreamWriter(outputFileName);
        await using var _ = sw.ConfigureAwait(false);

        _logger.LogInformation("Writing {Version} capabilities to {File}", version, outputFileName);

        var code = await Run(
            CreateProcess("capabilities", $"--version {version}"),
            p => sw.WriteLine(p),
            AppContext.BaseDirectory,
            true,
            cancellationToken
            ).ConfigureAwait(false);

        if (code != 0)
        {
            throw new RegoCompilationException(
                AppContext.BaseDirectory,
                $"Return code {code} didn't indicate success."
                );
        }
    }

    private async Task GetVersionInfo(CancellationToken cancellationToken)
    {
        var ver = new StringBuilder();

        var code = await Run(CreateProcess("version", null), p => ver.AppendLine(p), null, true, cancellationToken)
            .ConfigureAwait(false);

        if (code != 0)
        {
            throw new RegoCompilationException(
                AppContext.BaseDirectory,
                $"Return code {code} didn't indicate success.\nOutput: {ver}"
                );
        }

        var parts = ver.ToString().Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            if (part.StartsWith("Version: ", StringComparison.Ordinal))
            {
                var idx = "Version: ".Length;
                VersionInfo.Version = part[idx..];
                continue;
            }

            if (part.StartsWith("Build Commit: ", StringComparison.Ordinal))
            {
                var idx = "Build Commit: ".Length;
                VersionInfo.Commit = part[idx..];
                continue;
            }

            if (part.StartsWith("Build Timestamp: ", StringComparison.Ordinal))
            {
                var idx = "Build Timestamp: ".Length;
                VersionInfo.Timestamp = part[idx..];
                continue;
            }

            if (part.StartsWith("Go Version: ", StringComparison.Ordinal))
            {
                var idx = "Go Version: ".Length;
                VersionInfo.GoVersion = part[idx..];
                continue;
            }

            if (part.StartsWith("Platform: ", StringComparison.Ordinal))
            {
                var idx = "Platform: ".Length;
                VersionInfo.Platform = part[idx..];
                continue;
            }

            if (part.StartsWith("WebAssembly: ", StringComparison.Ordinal))
            {
                var idx = "WebAssembly: ".Length;
                VersionInfo.WebAssembly = part[idx..];
                continue;
            }
        }
    }

    public static async Task<OpaCliWrapper> Create(
        string opaCli = "opa",
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = new OpaCliWrapper(opaCli, logger);
            await result.GetVersionInfo(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (RegoCompilationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RegoCompilationException(AppContext.BaseDirectory, "Failed to run opa cli", ex);
        }
    }
}