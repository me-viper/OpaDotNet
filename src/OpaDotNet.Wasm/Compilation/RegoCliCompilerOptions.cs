using JetBrains.Annotations;

namespace OpaDotNet.Wasm.Compilation;

[PublicAPI]
public class RegoCliCompilerOptions
{
    public string? OpaToolPath { get; set; }

    public string? OutputPath { get; set; }
    
    public string? ExtraArguments { get; set; }
}