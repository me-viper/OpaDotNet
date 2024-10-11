using System.Text;

using JetBrains.Annotations;

using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Compilation.Cli;

[PublicAPI]
internal class OpaCliBuildArgs
{
    private static string Type => "wasm";

    public required string OutputFile { get; init; }

    public required string SourcePath { get; init; }

    public bool IsBundle { get; init; }

    public string? CapabilitiesFile { get; init; }

    public string? CapabilitiesVersion { get; init; }

    public IReadOnlySet<string>? Entrypoints { get; init; }

    public int OptimizationLevel { get; init; }

    public bool PruneUnused { get; init; }

    public string? ExtraArguments { get; init; }

    public bool Debug { get; init; }

    public IReadOnlySet<string>? Ignore { get; init; }

    public string? Revision { get; init; }

    public RegoVersion RegoVersion { get; init; }

    public bool FollowSymlinks { get; init; }

    public override string ToString()
    {
        var result = new StringBuilder($"-t {Type}");

        if (IsBundle)
            result.Append(" -b");

        if (Entrypoints?.Count > 0)
            result.Append(" " + string.Join(" ", Entrypoints.Select(p => $"-e {p}")));

        if (!string.IsNullOrWhiteSpace(CapabilitiesFile))
            result.Append($" --capabilities \"{CapabilitiesFile}\"");
        else
        {
            if (!string.IsNullOrWhiteSpace(CapabilitiesVersion))
                result.Append($" --capabilities {CapabilitiesVersion}");
        }

        result.Append($" --optimize {OptimizationLevel}");

        if (PruneUnused)
            result.Append(" --prune-unused");

        if (Debug)
            result.Append(" --debug");

        if (RegoVersion == RegoVersion.V1)
            result.Append(" --v1-compatible");

        if (!string.IsNullOrWhiteSpace(Revision))
            result.Append($" --revision \"{Revision}\"");

        if (FollowSymlinks)
            result.Append(" --follow-symlinks");

        if (Ignore is { Count: > 0 })
        {
            foreach (var s in Ignore)
                result.Append($" --ignore \"{s}\"");
        }

        result.Append($" -o \"{OutputFile}\"");

        if (!string.IsNullOrWhiteSpace(ExtraArguments))
            result.Append(" " + ExtraArguments);

        result.Append($" \"{SourcePath}\"");

        return result.ToString();
    }
}