using System.Text;

using Wasmtime;

namespace OpaDotNet.Wasm.Internal.V12;

internal class EngineImpl<TAbi> : V10.EngineImpl<TAbi>
    where TAbi : IOpaExportsAbi, IAbiInitializer<TAbi>
{
    public EngineImpl(
        Memory memory,
        Instance instance,
        JsonSerializerOptions? options = null) : base(memory, instance, options)
    {
    }

    public override Version AbiVersion => new(1, 2);

    public override nint Eval(ReadOnlySpan<char> inputJson, string? entrypoint = null)
    {
        var entrypointId = GetEntrypoint(entrypoint);
        
        Abi.HeapPtrSet(EvalHeapPtr);

        var inputLength = Encoding.UTF8.GetByteCount(inputJson);
        EnsureMemory(inputLength);

        var inputPtr = EvalHeapPtr;
        var bytesWritten = Memory.WriteString(inputPtr, inputJson, Encoding.UTF8);

        var resultHeapPtr = inputPtr + bytesWritten;
        var resultPtr = Abi.Eval(0, entrypointId, DataPtr, inputPtr, inputLength, resultHeapPtr, EvaluationOutputFormat.Json);

        Abi.HeapPtrSet(EvalHeapPtr);

        return resultPtr;
    }
}