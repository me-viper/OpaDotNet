using System.Text;

using OpaDotNet.Wasm.Extensions;

using Wasmtime;

namespace OpaDotNet.Wasm.Internal.V13;

internal class WasmPolicyEngine : WasmPolicyEngine<IOpaExportsAbi>, IUpdateDataExtension
{
    public override Version AbiVersion => new(1, 3);

    public WasmPolicyEngine(IOpaExportsAbi abi, Memory memory, Instance instance, JsonSerializerOptions? options = null)
        : base(abi, memory, instance, options)
    {
    }

    public override void SetData(ReadOnlySpan<char> dataJson)
    {
        base.SetData(dataJson);
        Abi.HeapBlocksStash();
    }

    public override void Reset()
    {
        Abi.HeapBlocksStashClear();
        Abi.HeapPtrSet(BasePtr);

        DataPtr = BasePtr;
        EvalHeapPtr = BasePtr;
    }

    public void UpdateDataPath(ReadOnlySpan<char> dataJson, IEnumerable<string> path)
    {
        ArgumentNullException.ThrowIfNull(path);
        
        Abi.HeapBlocksRestore();
        
        var valPtr = WriteJsonString(dataJson);
        var pathPtr = WriteJson(path);
        var result = Abi.ValueAddPath(DataPtr, pathPtr, valPtr);
        
        if (result != OpaResult.Ok)
            throw new OpaEvaluationException($"Failed to update data: {result}");
        
        Abi.ValueFree(pathPtr);
        Abi.HeapBlocksStash();
        
        EvalHeapPtr = Abi.HeapPrtGet();
    }

    public void RemoveDataPath(IEnumerable<string> path)
    {
        ArgumentNullException.ThrowIfNull(path);
        
        Abi.HeapBlocksRestore();
        
        var pathPtr = WriteJson(path);
        var result = Abi.ValueRemovePath(DataPtr, pathPtr);
        
        if (result != OpaResult.Ok)
            throw new OpaEvaluationException($"Failed to update data: {result}");
        
        Abi.ValueFree(pathPtr);
        Abi.HeapBlocksStash();
        
        EvalHeapPtr = Abi.HeapPrtGet();
    }

    public override nint Eval(ReadOnlySpan<char> inputJson, string? entrypoint = null)
    {
        var entrypointId = GetEntrypoint(entrypoint);

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