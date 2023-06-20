using Wasmtime;

namespace OpaDotNet.Wasm.Internal.V13;

internal class WasmPolicyEngine : WasmPolicyEngine<IOpaExportsAbi>
{
    private readonly V12.WasmPolicyEngine _inner;

    public override Version AbiVersion => new(1, 3);

    public WasmPolicyEngine(IOpaExportsAbi abi, Memory memory, Instance instance, JsonSerializerOptions? options = null)
        : base(abi, memory, instance, options)
    {
        _inner = new V12.WasmPolicyEngine(abi, memory, instance, options);
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

    public override nint Eval(ReadOnlySpan<char> inputJson, string? entrypoint = null)
    {
        return _inner.Eval(inputJson, entrypoint);
    }
}