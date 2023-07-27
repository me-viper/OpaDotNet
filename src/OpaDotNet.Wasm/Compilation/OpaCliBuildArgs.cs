using System.Text;

namespace OpaDotNet.Wasm.Compilation;

internal class OpaCliBuildArgs
{
    private string Type => "wasm";

    public required string OutputFile { get; init; }

    public required string SourcePath { get; init; }

    public bool IsBundle { get; init; }

    public string? CapabilitiesFile { get; init; }

    public HashSet<string>? Entrypoints { get; init; }

    public string? ExtraArguments { get; init; }

    public override string ToString()
    {
        var result = new StringBuilder($"-t {Type}");

        if (IsBundle)
            result.Append(" -b");

        if (Entrypoints?.Count > 0)
            result.Append(" " + string.Join(" ", Entrypoints.Select(p => $"-e {p}")));

        if (!string.IsNullOrWhiteSpace(CapabilitiesFile))
            result.Append($" --capabilities {CapabilitiesFile}");

        result.Append($" -o {OutputFile}");

        if (!string.IsNullOrWhiteSpace(ExtraArguments))
            result.Append(" " + ExtraArguments);

        result.Append($" {SourcePath}");

        return result.ToString();
    }
}