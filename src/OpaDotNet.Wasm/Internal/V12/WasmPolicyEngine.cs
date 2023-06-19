using System.Text;

using Wasmtime;

namespace OpaDotNet.Wasm.Internal.V12;

internal class WasmPolicyEngine : WasmPolicyEngine<IOpaExportsAbi>
{
    public WasmPolicyEngine(
        IOpaExportsAbi abi,
        Memory memory,
        Instance instance,
        JsonSerializerOptions? options = null) : base(abi, memory, instance, options)
    {
    }

    public override Version AbiVersion => new(1, 2);
    
    public override nint Eval(ReadOnlySpan<char> inputJson, string? entrypoint = null)
    {
        var entrypointId = GetEntrypoint(entrypoint);
        
        var inputLength = Encoding.UTF8.GetByteCount(inputJson);
        var delta = EvalHeapPtr + inputLength - Memory.GetLength();
            
        if (delta > 0)
        {
            var pages = Math.Ceiling((double)delta / Memory.PageSize);
            Memory.Grow((long)pages);
        }
        
        var inputPtr = EvalHeapPtr;
        var bytesWritten = Memory.WriteString(inputPtr, inputJson, Encoding.UTF8);
        
        var resultHeapPtr = inputPtr + bytesWritten;
        var resultPtr = Abi.Eval(0, entrypointId, DataPtr, inputPtr, inputLength, resultHeapPtr, EvaluationOutputFormat.Json);
        
        Abi.HeapPtrSet(EvalHeapPtr);
        
        return resultPtr;
    }
}