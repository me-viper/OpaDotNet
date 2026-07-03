using System.Text;

using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Compilation.Cli;

internal class OpaCliCheckArgs(CheckParameters parameters)
{
    public required string SourcePath { get; init; }

    public string? CapabilitiesFile { get; init; }

    public override string ToString()
    {
        var result = new StringBuilder();

        result.Append(SourcePath);

        if (parameters.IsBundle)
            result.Append(" -b");

        if (!string.IsNullOrWhiteSpace(CapabilitiesFile))
            result.Append($" --capabilities \"{CapabilitiesFile}\"");
        else
        {
            if (!string.IsNullOrWhiteSpace(parameters.CapabilitiesVersion))
                result.Append($" --capabilities {parameters.CapabilitiesVersion}");
        }

        result.Append($" --format {parameters.Format.ToString("G").ToLowerInvariant()}");

        if (parameters.Ignore is { Count: > 0 })
        {
            foreach (var s in parameters.Ignore)
                result.Append($" --ignore \"{s}\"");
        }

        if (parameters.MaxErrors.HasValue)
            result.Append($" --max-errors {parameters.MaxErrors.Value}");

        if (!string.IsNullOrWhiteSpace(parameters.Schema))
            result.Append($" --schema {parameters.Schema}");

        if (parameters.Strict)
            result.Append(" --strict");

        switch (parameters.RegoVersion)
        {
            case RegoVersion.V0:
                result.Append(" --v0-compatible");
                break;

            case RegoVersion.V0CompatV1:
                result.Append(" --v0-v1");
                break;
        }

        return result.ToString();
    }
}