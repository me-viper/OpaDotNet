﻿namespace OpaDotNet.Wasm.Internal.V13;

/// <summary>
/// OPA WASM ABI v1.3
/// </summary>
[PublicAPI]
internal interface IOpaExportsAbi : V12.IOpaExportsAbi
{
    /// <summary>
    /// Free a value such as one generated by <see cref="V10.IOpaExportsAbi.ValueParse"/>
    /// or <see cref="V10.IOpaExportsAbi.JsonParse"/> reference.
    /// </summary>
    /// <param name="ptr">Address of the reference</param>
    void ValueFree(nint ptr);

    /// <summary>
    /// Stash free heap blocks in a shadow heap to enable <see cref="V10.IOpaExportsAbi.Eval"/> to allocate only blocks
    /// that it can subsequently free with a call to <see cref="V10.IOpaExportsAbi.HeapPtrSet"/>.
    /// The caller should subsequently call <see cref="V10.IOpaExportsAbi.HeapPrtGet"/> and store the value
    /// to save before calling <see cref="HeapBlocksRestore"/>
    /// </summary>
    void HeapBlocksStash();

    /// <summary>
    /// Restore heap blocks stored by <see cref="HeapBlocksStash"/> to the heap.
    /// This should only be called after a <see cref="V10.IOpaExportsAbi.HeapPtrSet"/> to the a
    /// heap pointer recorded by <see cref="V10.IOpaExportsAbi.HeapPrtGet"/> after the previous
    /// call to <see cref="HeapBlocksStash"/>.
    /// </summary>
    void HeapBlocksRestore();

    /// <summary>
    /// Drop all heap blocks saved by <see cref="HeapBlocksStash"/>..
    /// This leaks memory in the VM unless the caller subsequently
    /// invokes <see cref="V10.IOpaExportsAbi.HeapPtrSet"/> to a value taken
    /// prior to calling <see cref="HeapBlocksStash"/>.
    /// </summary>
    void HeapBlocksStashClear();
}