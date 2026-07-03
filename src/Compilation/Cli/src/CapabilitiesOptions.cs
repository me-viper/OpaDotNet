namespace OpaDotNet.Compilation.Cli;

internal class CapabilitiesOptions
{
    public string? CapabilitiesFilePath { get; set; }

    public ReadOnlyMemory<byte> CapabilitiesBytes { get; set; } = Memory<byte>.Empty;

    public string? CapabilitiesVersion { get; set; }
}