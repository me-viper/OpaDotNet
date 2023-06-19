using Wasmtime;

namespace OpaDotNet.Wasm.Internal.V10;

internal class OpaExportsAbi : IOpaExportsAbi
{
    private static Version Version { get; } = new(1, 0);
    
    private readonly Func<int> _entrypoint;
    private readonly Func<int> _builtins;
    private readonly Func<int, int> _malloc;
    private readonly Action<int> _free;
    
    private readonly Func<int, int, int> _jsonParse;
    private readonly Func<int, int> _jsonDump;
    private readonly Func<int, int, int> _valueParse;
    private readonly Func<int, int> _valueDump;
    
    private readonly Func<int, int, int, int> _valueAddPath;
    private readonly Func<int, int, int> _valueRemovePath;
    
    private readonly Action<int> _prtSet;
    private readonly Func<int> _prtGet;
    
    private readonly Func<int> _createContext;
    private readonly Action<int, int> _setInput;
    private readonly Action<int, int> _setData;
    private readonly Action<int, int> _setEntrypoint;
    private readonly Func<int, int> _getResult;
    private readonly Func<int, int> _eval;

    public OpaExportsAbi(Instance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        
        _entrypoint = instance.GetFunction<int>("entrypoints") 
            ?? throw new ExportResolutionException(Version, "entrypoints");
        
        _builtins = instance.GetFunction<int>("builtins")
            ?? throw new ExportResolutionException(Version, "builtins");
        
        _malloc = instance.GetFunction<int, int>("opa_malloc")
            ?? throw new ExportResolutionException(Version, "opa_malloc");
        
        _free = instance.GetAction<int>("opa_free")
            ?? throw new ExportResolutionException(Version, "opa_free");
        
        _jsonParse = instance.GetFunction<int, int, int>("opa_json_parse")
            ?? throw new ExportResolutionException(Version, "opa_json_parse");
        
        _jsonDump = instance.GetFunction<int, int>("opa_json_dump")
            ?? throw new ExportResolutionException(Version, "opa_json_dump");
        
        _valueParse = instance.GetFunction<int, int, int>("opa_value_parse")
            ?? throw new ExportResolutionException(Version, "opa_value_parse");
        
        _valueDump = instance.GetFunction<int, int>("opa_value_dump")
            ?? throw new ExportResolutionException(Version, "opa_value_dump");
        
        _valueAddPath = instance.GetFunction<int, int, int, int>("opa_value_add_path")
            ?? throw new ExportResolutionException(Version, "opa_value_add_path");
        
        _valueRemovePath = instance.GetFunction<int, int, int>("opa_value_remove_path")
            ?? throw new ExportResolutionException(Version, "opa_value_remove_path");
        
        _prtSet = instance.GetAction<int>("opa_heap_ptr_set")
            ?? throw new ExportResolutionException(Version, "opa_heap_ptr_set");
        
        _prtGet = instance.GetFunction<int>("opa_heap_ptr_get")
            ?? throw new ExportResolutionException(Version, "opa_heap_ptr_get");
        
        _createContext = instance.GetFunction<int>("opa_eval_ctx_new")
            ?? throw new ExportResolutionException(Version, "opa_eval_ctx_new");
        
        _setInput = instance.GetAction<int, int>("opa_eval_ctx_set_input")
            ?? throw new ExportResolutionException(Version, "opa_eval_ctx_set_input");
        
        _setData = instance.GetAction<int, int>("opa_eval_ctx_set_data")
            ?? throw new ExportResolutionException(Version, "opa_eval_ctx_set_data");
        
        _setEntrypoint = instance.GetAction<int, int>("opa_eval_ctx_set_entrypoint")
            ?? throw new ExportResolutionException(Version, "opa_eval_ctx_set_entrypoint");
        
        _eval = instance.GetFunction<int, int>("eval")
            ?? throw new ExportResolutionException(Version, "eval");
        
        _getResult = instance.GetFunction<int, int>("opa_eval_ctx_get_result")
            ?? throw new ExportResolutionException(Version, "opa_eval_ctx_get_result");
    }
    
    public nint Entrypoints()
    {
        return _entrypoint();
    }

    public nint Builtins()
    {
        return _builtins();
    }
    
    public nint Malloc(int size)
    {
        return _malloc(size);
    }

    public void Free(nint ptr)
    {
        _free(ptr.ToInt32());
    }

    public nint JsonParse(nint ptr, int size)
    {
        return _jsonParse(ptr.ToInt32(), size);
    }

    public nint JsonDump(nint ptr)
    {
        return _jsonDump(ptr.ToInt32());
    }

    public nint ValueParse(nint ptr, int size)
    {
        return _valueParse(ptr.ToInt32(), size);
    }

    public nint ValueDump(nint ptr)
    {
        return _valueDump(ptr.ToInt32());
    }
    
    public OpaResult ValueAddPath(nint baseValuePtr, nint pathValuePtr, nint valuePtr)
    {
        return (OpaResult)_valueAddPath(baseValuePtr.ToInt32(), pathValuePtr.ToInt32(), valuePtr.ToInt32());
    }

    public OpaResult ValueRemovePath(nint baseValuePtr, nint pathValuePtr)
    {
        return (OpaResult)_valueRemovePath(baseValuePtr.ToInt32(), pathValuePtr.ToInt32());
    }

    public nint HeapPrtGet()
    {
        return _prtGet();
    }

    public void HeapPtrSet(nint ptr)
    {
        _prtSet(ptr.ToInt32());
    }

    public nint ContextCreate()
    {
        return _createContext();
    }

    public void ContextSetInput(nint contextPtr, nint inputPtr)
    {
        _setInput(contextPtr.ToInt32(), inputPtr.ToInt32()); 
    }

    public void ContextSetData(nint contextPtr, nint dataPtr)
    {
        _setData(contextPtr.ToInt32(), dataPtr.ToInt32());
    }

    public void ContextSetEntrypoint(nint contextPtr, int entrypointId)
    {
        _setEntrypoint(contextPtr.ToInt32(), entrypointId);
    }

    public nint ContextGetResult(nint contextPtr)
    {
        return _getResult(contextPtr.ToInt32());
    }

    public nint Eval(nint contextPtr)
    {
        return _eval(contextPtr.ToInt32());
    }
}