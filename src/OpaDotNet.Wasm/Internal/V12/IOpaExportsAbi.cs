﻿namespace OpaDotNet.Wasm.Internal.V12;

internal interface IOpaExportsAbi : V10.IOpaExportsAbi
{
    /// <summary>
    /// Policy evaluation method.
    /// </summary>
    /// <param name="reserved">Reserved for future use and must be 0</param>
    /// <param name="entrypointId">Entrypoint ID</param>
    /// <param name="dataPtr">Address of data in memory</param>
    /// <param name="inputPtr">Address if input JSON string in memory</param>
    /// <param name="inputLength">Address input JSON string in memory</param>
    /// <param name="heapPtr">Heap address to use</param>
    /// <param name="format">Output format</param>
    /// <returns>Address to the serialised result value</returns>
    nint Eval(int reserved, int entrypointId, nint dataPtr, nint inputPtr, int inputLength, nint heapPtr, EvaluationOutputFormat format);
}