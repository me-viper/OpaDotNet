using System.Text;

using OpaDotNet.Wasm.Internal.V10;

using Wasmtime;

namespace OpaDotNet.Wasm.Internal;

internal abstract class WasmPolicyEngine<TAbi> : IWasmPolicyEngine
    where TAbi : IOpaExportsAbi
{
    public abstract Version AbiVersion { get; }

    protected Instance Instance { get; }

    public IReadOnlyDictionary<string, int> Entrypoints { get; }

    public IReadOnlyDictionary<int, string> Builtins { get; }

    protected nint BasePtr { get; }

    protected nint DataPtr { get; set; }

    protected nint EvalHeapPtr { get; set; }

    protected TAbi Abi { get; }

    protected Memory Memory { get; }

    protected JsonSerializerOptions JsonOptions { get; }

    protected WasmPolicyEngine(
        TAbi abi,
        Memory memory,
        Instance instance,
        JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(abi);
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(instance);

        Abi = abi;
        Memory = memory;
        Instance = instance;
        JsonOptions = options ?? JsonSerializerOptions.Default;

        var epPtr = Abi.Entrypoints();
        var epJsonPtr = Abi.JsonDump(epPtr);
        Entrypoints = Memory.ReadNullTerminatedJson<Dictionary<string, int>>(epJsonPtr, JsonOptions)
            ?? throw new OpaRuntimeException("Failed to deserialize entrypoints value");
        Abi.Free(epJsonPtr);

        var builtinsPtr = Abi.Builtins();
        var builtinsJsonPtr = Abi.JsonDump(builtinsPtr);
        var builtins = Memory.ReadNullTerminatedJson<Dictionary<string, int>>(builtinsJsonPtr)
            ?? throw new OpaRuntimeException("Failed to deserialize builtins value");
        Builtins = builtins.ToDictionary(p => p.Value, p => p.Key);
        Abi.Free(builtinsJsonPtr);

        BasePtr = Abi.HeapPrtGet();
        EvalHeapPtr = BasePtr;
        DataPtr = BasePtr;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
        }
    }

    protected int GetEntrypoint(string? entrypoint)
    {
        var entrypointId = 0;

        if (entrypoint != null && !Entrypoints.TryGetValue(entrypoint, out entrypointId))
            throw new OpaEvaluationException($"Unknown entrypoint {entrypoint}");

        return entrypointId;
    }
    
    protected void EnsureMemory(int size)
    {
        var delta = EvalHeapPtr + size - Memory.GetLength();

        if (delta > 0)
        {
            var pages = Math.Ceiling((double)delta / Memory.PageSize);
            Memory.Grow((long)pages);
        }
    }

    public abstract nint Eval(ReadOnlySpan<char> inputJson, string? entrypoint = null);

    public virtual void SetData(ReadOnlySpan<char> dataJson)
    {
        Abi.HeapPtrSet(BasePtr);

        if (dataJson.IsEmpty)
            return;

        DataPtr = WriteJsonString(dataJson);
        EvalHeapPtr = Abi.HeapPrtGet();
    }

    public virtual void Reset()
    {
        DataPtr = BasePtr;
        EvalHeapPtr = BasePtr;
        Abi.HeapPtrSet(BasePtr);
    }

    public virtual nint WriteJsonString(ReadOnlySpan<char> data)
    {
        var dataLength = Encoding.UTF8.GetByteCount(data);
        var dataPtr = Abi.Malloc(dataLength);
        var bytesWritten = Memory.WriteString(dataPtr, data, Encoding.UTF8);

        return Abi.JsonParse(dataPtr, bytesWritten);
    }

    public virtual nint WriteJson<T>(T data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var s = JsonSerializer.Serialize(data, JsonOptions);
        return WriteJsonString(s);
    }

    public virtual string ReadJsonString(nint ptr)
    {
        var jsonAdr = Abi.JsonDump(ptr);
        return Memory.ReadNullTerminatedString(jsonAdr);
    }

    public virtual T ReadJson<T>(nint ptr)
    {
        var jsonAdr = Abi.JsonDump(ptr);
        var result = Memory.ReadNullTerminatedJson<T>(jsonAdr, JsonOptions);

        if (result == null)
            throw new OpaEvaluationException($"Failed to read json from {jsonAdr}");

        return result;
    }
}