using OpaDotNet.Wasm.Features;

using Wasmtime;

namespace OpaDotNet.Wasm.Internal.V13;

internal class EngineImpl<TAbi> : V12.EngineImpl<TAbi>, IUpdateDataFeature
    where TAbi : IOpaExportsAbi, IAbiInitializer<TAbi>
{
    public override Version AbiVersion => new(1, 3);

    public EngineImpl(Memory memory, Instance instance, JsonSerializerOptions? options = null)
        : base(memory, instance, options)
    {
    }

    // Base implementations reset heap to the pointer taken before previous stash, so it must be dropped first.
    public override void SetData(ReadOnlySpan<char> dataJson)
    {
        Abi.HeapBlocksStashClear();
        base.SetData(dataJson);
        Abi.HeapBlocksStash();
    }

    public override void SetDataFromBytes(ReadOnlySpan<byte> data)
    {
        Abi.HeapBlocksStashClear();
        base.SetDataFromBytes(data);
        Abi.HeapBlocksStash();
    }

    public override void SetData(Stream? data)
    {
        Abi.HeapBlocksStashClear();
        base.SetData(data);
        Abi.HeapBlocksStash();
    }

    public override void Reset()
    {
        Abi.HeapBlocksStashClear();
        base.Reset();
    }

    void IUpdateDataFeature.UpdateDataPath(ReadOnlySpan<char> dataJson, IEnumerable<string> path)
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

    void IUpdateDataFeature.RemoveDataPath(IEnumerable<string> path)
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
}