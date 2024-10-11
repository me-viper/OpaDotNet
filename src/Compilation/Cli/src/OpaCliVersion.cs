using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Compilation.Cli;

internal record OpaCliVersion : RegoCompilerVersion
{
    public string? Timestamp { get; set; }

    public string? WebAssembly { get; set; }
}