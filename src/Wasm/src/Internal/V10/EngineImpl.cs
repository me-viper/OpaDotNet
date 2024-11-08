using System.Text;

using Wasmtime;

namespace OpaDotNet.Wasm.Internal.V10;

internal class EngineImpl<TAbi> : WasmPolicyEngine<TAbi>
    where TAbi : IOpaExportsAbi, IAbiInitializer<TAbi>
{
    public EngineImpl(Memory memory, Instance instance, JsonSerializerOptions? options = null)
        : base(memory, instance, options)
    {
    }

    public override Version AbiVersion => new(1, 0);

    public override nint Eval(ReadOnlySpan<char> inputJson, string? entrypoint = null)
    {
        var entrypointId = GetEntrypoint(entrypoint);

        Abi.HeapPtrSet(EvalHeapPtr);

        var inputLength = Encoding.UTF8.GetByteCount(inputJson);
        EnsureMemory(inputLength);

        try
        {
            var context = Abi.ContextCreate();

            var parsedAdr = WriteJsonString(inputJson);
            Abi.ContextSetInput(context, parsedAdr);
            Abi.ContextSetData(context, DataPtr);
            Abi.ContextSetEntrypoint(context, entrypointId);
            Abi.Eval(context);

            var resultAdr = Abi.ContextGetResult(context);

            return Abi.JsonDump(resultAdr);
        }
        finally
        {
            Abi.HeapPtrSet(EvalHeapPtr);
        }
    }
}