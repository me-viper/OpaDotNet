using System.Text;

using OpaDotNet.Wasm.Rego;

using Wasmtime;

namespace OpaDotNet.Wasm.Internal;

internal abstract class WasmPolicyEngine<TAbi> : IWasmPolicyEngine
    where TAbi : V10.IOpaExportsAbi, IAbiInitializer<TAbi>
{
    private JsonSerializerOptions JsonOptions { get; }

    protected nint BasePtr { get; }

    protected nint DataPtr { get; set; }

    protected nint EvalHeapPtr { get; set; }

    protected TAbi Abi { get; }

    protected Memory Memory { get; }

    public abstract Version AbiVersion { get; }

    public IReadOnlyDictionary<string, int> Entrypoints { get; }

    public IReadOnlyDictionary<int, string> Builtins { get; }

    protected WasmPolicyEngine(
        Memory memory,
        Instance instance,
        JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(memory);
        ArgumentNullException.ThrowIfNull(instance);

        Abi = TAbi.Initialize(instance);
        Memory = memory;
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

    // ReSharper disable once VirtualMemberNeverOverridden.Global
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

    public virtual void SetDataFromBytes(ReadOnlySpan<byte> data)
    {
        Abi.HeapPtrSet(BasePtr);

        if (data.IsEmpty)
            return;

        var dataPtr = Abi.Malloc(data.Length);
        Memory.WriteBytes(dataPtr, data);
        DataPtr = Abi.JsonParse(dataPtr, data.Length);
        Abi.Free(dataPtr);

        EvalHeapPtr = Abi.HeapPrtGet();
    }

    public virtual void SetData(Stream? data)
    {
        Abi.HeapPtrSet(BasePtr);

        if (data == null || data.Length == 0)
            return;

        var len = (int)data.Length;

        var dataPtr = Abi.Malloc(len);
        var bytesRead = data.Read(Memory.GetSpan(dataPtr, len));
        DataPtr = Abi.JsonParse(dataPtr, bytesRead);
        Abi.Free(dataPtr);

        EvalHeapPtr = Abi.HeapPrtGet();
    }

    public string DumpData()
    {
        var jsonPtr = Abi.JsonDump(DataPtr);
        return Memory.ReadNullTerminatedString(jsonPtr);
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
        var result = Abi.JsonParse(dataPtr, bytesWritten);
        Abi.Free(dataPtr);

        return result;
    }

    public virtual nint WriteValueString(ReadOnlySpan<char> data)
    {
        if (data.IsEmpty)
            return WriteNullValue();

        var dataLength = Encoding.UTF8.GetByteCount(data);
        var dataPtr = Abi.Malloc(dataLength);
        var bytesWritten = Memory.WriteString(dataPtr, data, Encoding.UTF8);
        var result = Abi.ValueParse(dataPtr, bytesWritten);
        Abi.Free(dataPtr);

        return result;
    }

    public virtual nint WriteValue<T>(T? data)
    {
        if (data == null)
            return WriteNullValue();

        var s = JsonSerializer.Serialize(data, JsonOptions);
        s = RegoValueHelper.JsonToRegoValue(s);
        return WriteValueString(s);
    }

    private nint WriteNullValue()
    {
        var dataPtr = Abi.Malloc(4);
        Memory.WriteInt32(dataPtr, 0);
        var result = Abi.ValueParse(dataPtr, 4);
        Abi.Free(dataPtr);

        return result;
    }

    public virtual nint WriteJson<T>(T? data)
    {
        var s = JsonSerializer.Serialize(data, JsonOptions);
        return WriteJsonString(s);
    }

    public virtual string ReadJsonString(nint ptr)
    {
        var jsonAdr = Abi.JsonDump(ptr);
        return Memory.ReadNullTerminatedString(jsonAdr);
    }

    public virtual string ReadValueString(nint ptr)
    {
        var jsonAdr = Abi.ValueDump(ptr);
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