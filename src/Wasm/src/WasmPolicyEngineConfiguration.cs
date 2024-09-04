using Wasmtime;

namespace OpaDotNet.Wasm;

internal record WasmPolicyEngineConfiguration
{
    public required Engine Engine { get; init; }

    public required Linker Linker { get; init; }

    public required Store Store { get; init; }

    public required Memory Memory { get; init; }

    public required Module Module { get; init; }

    public required WasmPolicyEngineOptions Options { get; init; }

    public required ILogger<IOpaEvaluator> Logger { get; init; }

    public required IOpaImportsAbi Imports { get; init; }
}