using Wasmtime;

namespace OpaDotNet.Wasm.Internal;

internal record WasmPolicyEngineConfiguration
{
    public required Engine Engine { get; init; }

    public required Linker Linker { get; init; }

    public required Store Store { get; init; }

    public required Memory Memory { get; init; }

    public required Module Module { get; init; }

    public required WasmPolicyEngineOptions Options { get; init; }

    public required IOpaImportsAbi Imports { get; init; }
}