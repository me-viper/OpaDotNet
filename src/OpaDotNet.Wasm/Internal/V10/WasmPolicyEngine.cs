using Wasmtime;

namespace OpaDotNet.Wasm.Internal.V10;

internal class WasmPolicyEngine : WasmPolicyEngine<IOpaExportsAbi>
{
    public WasmPolicyEngine(IOpaExportsAbi abi, Memory memory, Instance instance, JsonSerializerOptions? options = null)
        : base(abi, memory, instance, options)
    {
    }

    public override Version AbiVersion => new(1, 0);

    public override nint Eval(ReadOnlySpan<char> inputJson, string? entrypoint = null)
    {
        var entrypointId = GetEntrypoint(entrypoint);

        Abi.HeapPtrSet(EvalHeapPtr);

        var context = Abi.ContextCreate();

        var parsedAdr = WriteJsonString(inputJson);
        Abi.ContextSetInput(context, parsedAdr);
        Abi.ContextSetData(context, DataPtr);
        Abi.ContextSetEntrypoint(context, entrypointId);
        Abi.Eval(context);

        var resultAdr = Abi.ContextGetResult(context);

        return Abi.JsonDump(resultAdr);
    }
}