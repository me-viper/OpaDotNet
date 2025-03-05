using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Compilation.Cli;

[PublicAPI]
internal record OpaCliVersion : RegoCompilerVersion
{
    public string? Timestamp { get; set; }

    public string? WebAssembly { get; set; }
}