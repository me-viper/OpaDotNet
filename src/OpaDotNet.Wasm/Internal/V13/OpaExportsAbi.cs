using Wasmtime;

namespace OpaDotNet.Wasm.Internal.V13;

internal class OpaExportsAbi : V12.OpaExportsAbi, IOpaExportsAbi
{
    private static Version Version { get; } = new(1, 3);

    private readonly Action<int> _valueFree;

    private readonly Action _heapBlocksStash;

    private readonly Action _heapBlocksRestore;

    private readonly Action _heapStashClear;

    public OpaExportsAbi(Instance instance) : base(instance)
    {
        _valueFree = instance.GetAction<int>("opa_value_free")
            ?? throw new ExportResolutionException(Version, "opa_value_free");

        _heapBlocksStash = instance.GetAction("opa_heap_blocks_stash")
            ?? throw new ExportResolutionException(Version, "opa_heap_blocks_stash");

        _heapBlocksRestore = instance.GetAction("opa_heap_blocks_restore")
            ?? throw new ExportResolutionException(Version, "opa_heap_blocks_restore");

        _heapStashClear = instance.GetAction("opa_heap_stash_clear")
            ?? throw new ExportResolutionException(Version, "opa_heap_stash_clear");
    }

    public void ValueFree(nint ptr)
    {
        _valueFree(ptr.ToInt32());
    }

    public void HeapBlocksStash()
    {
        _heapBlocksStash();
    }

    public void HeapBlocksRestore()
    {
        _heapBlocksRestore();
    }

    public void HeapBlocksStashClear()
    {
        _heapStashClear();
    }
}