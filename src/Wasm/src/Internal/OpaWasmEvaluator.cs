﻿using OpaDotNet.Wasm.Features;
using OpaDotNet.Wasm.Rego;

using Wasmtime;

namespace OpaDotNet.Wasm.Internal;

using EngineV10 = V10.EngineImpl<V10.OpaExportsAbi>;
using EngineV12 = V12.EngineImpl<V12.OpaExportsAbi>;
using EngineV13 = V13.EngineImpl<V13.OpaExportsAbi>;

internal sealed class OpaWasmEvaluator : IOpaEvaluator
{
    private readonly Engine _engine;

    private readonly Linker _linker;

    private readonly Store _store;

    private readonly Module _module;

    private readonly WasmPolicyEngineOptions _engineOptions;

    private JsonSerializerOptions JsonOptions => _engineOptions.SerializationOptions;

    private readonly IWasmPolicyEngine _abi;

    private readonly Memory _memory;

    private readonly IOpaImportsAbi _importsAbi;

    public Version AbiVersion => _abi.AbiVersion;

    public Version PolicyAbiVersion { get; }

    internal OpaWasmEvaluator(WasmPolicyEngineConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _engineOptions = configuration.Options;

        _engine = configuration.Engine;
        _linker = configuration.Linker;
        _store = configuration.Store;
        _module = configuration.Module;
        _memory = configuration.Memory;
        _importsAbi = configuration.Imports;

        SetupLinker(configuration.Imports);

        var instance = _linker.Instantiate(_store, _module);

        var abiMajorVar = instance.GetGlobal("opa_wasm_abi_version")?.GetValue();
        var abiMinorVar = instance.GetGlobal("opa_wasm_abi_minor_version")?.GetValue();

        if (abiMajorVar is not int abiMajor)
            throw new OpaRuntimeException("Failed to get value from opa_wasm_abi_version global");

        if (abiMinorVar is not int abiMinor)
            throw new OpaRuntimeException("Failed to get value from opa_wasm_abi_minor_version global");

        PolicyAbiVersion = new Version(abiMajor, abiMinor);

        var abiVersion = configuration.Options.MaxAbiVersion ?? PolicyAbiVersion;

        if (abiVersion > PolicyAbiVersion)
            abiVersion = PolicyAbiVersion;

        if (abiVersion < new Version(1, 2))
        {
            _abi = new EngineV10(
                _memory,
                instance,
                JsonOptions
                );
        }
        else if (abiVersion == new Version(1, 2))
        {
            _abi = new EngineV12(
                _memory,
                instance,
                JsonOptions
                );
        }
        else if (abiVersion >= new Version(1, 3))
        {
            _abi = new EngineV13(
                _memory,
                instance,
                JsonOptions
                );
        }

        if (_abi == null)
            throw new OpaRuntimeException($"Failed to initialize ABI for {PolicyAbiVersion}");
    }

    private void SetupLinker(IOpaImportsAbi imports)
    {
        BuiltinContext Context(int id, int ctx = 0)
        {
            if (!_abi.Builtins.TryGetValue(id, out var funcName))
                throw new OpaRuntimeException($"Failed to resolve builtin with ID {id}");

            return new BuiltinContext
            {
                FunctionName = funcName,
                OpaContext = ctx,
                JsonSerializerOptions = JsonOptions,
                StrictBuiltinErrors = _engineOptions.StrictBuiltinErrors,
            };
        }

        _linker.Define("env", "memory", _memory);

        _linker.Define(
            "env",
            "opa_abort",
            Function.FromCallback(_store, (int ptr) => imports.Abort(_abi.ReadString(ptr)))
            );

        _linker.Define(
            "env",
            "opa_println",
            Function.FromCallback(_store, (int ptr) => imports.PrintLn(ReadJson<string>(ptr)))
            );

        _linker.Define(
            "env",
            "opa_builtin0",
            Function.FromCallback(
                _store,
                (int id, int ctx) =>
                {
                    var result = imports.Func(Context(id, ctx));
                    return WriteValue(result).ToInt32();
                }
                )
            );

        _linker.Define(
            "env",
            "opa_builtin1",
            Function.FromCallback(
                _store,
                (int id, int ctx, int arg1) =>
                {
                    var a1 = new BuiltinArg(p => ValueOrJson(p, arg1), JsonOptions);
                    var result = imports.Func(Context(id, ctx), a1);
                    return WriteValue(result).ToInt32();
                }
                )
            );

        _linker.Define(
            "env",
            "opa_builtin2",
            Function.FromCallback(
                _store,
                (int id, int ctx, int arg1, int arg2) =>
                {
                    var a1 = new BuiltinArg(p => ValueOrJson(p, arg1), JsonOptions);
                    var a2 = new BuiltinArg(p => ValueOrJson(p, arg2), JsonOptions);
                    var result = imports.Func(Context(id, ctx), a1, a2);
                    return WriteValue(result).ToInt32();
                }
                )
            );

        _linker.Define(
            "env",
            "opa_builtin3",
            Function.FromCallback(
                _store,
                (int id, int ctx, int arg1, int arg2, int arg3) =>
                {
                    var a1 = new BuiltinArg(p => ValueOrJson(p, arg1), JsonOptions);
                    var a2 = new BuiltinArg(p => ValueOrJson(p, arg2), JsonOptions);
                    var a3 = new BuiltinArg(p => ValueOrJson(p, arg3), JsonOptions);
                    var result = imports.Func(Context(id, ctx), a1, a2, a3);
                    return WriteValue(result).ToInt32();
                }
                )
            );

        _linker.Define(
            "env",
            "opa_builtin4",
            Function.FromCallback(
                _store,
                (int id, int ctx, int arg1, int arg2, int arg3, int arg4) =>
                {
                    var a1 = new BuiltinArg(p => ValueOrJson(p, arg1), JsonOptions);
                    var a2 = new BuiltinArg(p => ValueOrJson(p, arg2), JsonOptions);
                    var a3 = new BuiltinArg(p => ValueOrJson(p, arg3), JsonOptions);
                    var a4 = new BuiltinArg(p => ValueOrJson(p, arg4), JsonOptions);
                    var result = imports.Func(Context(id, ctx), a1, a2, a3, a4);
                    return WriteValue(result).ToInt32();
                }
                )
            );

        string ValueOrJson(RegoValueFormat t, int arg)
        {
            return t == RegoValueFormat.Value ? ReadValueString(arg) : ReadJsonString(arg);
        }
    }

    internal string DumpData()
    {
        return _abi.DumpData();
    }

    // ReSharper disable once UnusedMember.Local
    private nint WriteJsonString(ReadOnlySpan<char> data)
    {
        return _abi.WriteJsonString(data);
    }

    // ReSharper disable once UnusedMember.Local
    private nint WriteJson<T>(T? data)
    {
        return _abi.WriteJson(data);
    }

    private nint WriteValue<T>(T? data)
    {
        return _abi.WriteValue(data);
    }

    private string ReadJsonString(nint ptr)
    {
        return _abi.ReadJsonString(ptr);
    }

    private string ReadValueString(nint ptr)
    {
        var s = _abi.ReadValueString(ptr);
        return RegoValueHelper.JsonFromRegoValue(s);
    }

    private T ReadJson<T>(nint ptr)
    {
        return _abi.ReadJson<T>(ptr);
    }

    public void SetDataFromRawJson(ReadOnlySpan<char> dataJson)
    {
        _abi.SetData(dataJson);
    }

    public void SetDataFromStream(Stream? utf8Json)
    {
        _abi.SetData(utf8Json);
    }

    public void SetData<T>(T? data) where T : class
    {
        if (data == null)
            _abi.SetData(ReadOnlySpan<char>.Empty);

        var s = JsonSerializer.Serialize(data, JsonOptions);
        _abi.SetData(s);
    }

    internal void SetDataFromBytes(ReadOnlySpan<byte> utf8Json)
    {
        _abi.SetDataFromBytes(utf8Json);
    }

    public void Reset()
    {
        _abi.Reset();
        _importsAbi.Reset();
    }

    public bool TryGetFeature<TFeature>([MaybeNullWhen(false)] out TFeature feature)
        where TFeature : class, IOpaEvaluatorFeature
    {
        feature = _abi as TFeature;
        return feature != null;
    }

    public PolicyEvaluationResult<bool> EvaluatePredicate<TInput>(TInput input, string? entrypoint = null)
    {
        var result = EvalInternal<TInput, PolicyEvaluationResult<bool>[]>(input, entrypoint);

        if (result == null || result.Length == 0)
            return new PolicyEvaluationResult<bool> { Result = false };

        return result[0];
    }

    public PolicyEvaluationResult<TOutput?> EvaluateOrDefault<TInput, TOutput>(TInput input, string? entrypoint = null)
    {
        var result = EvalInternal<TInput, PolicyEvaluationResult<TOutput?>[]>(input, entrypoint);

        if (result == null || result.Length == 0)
            return new PolicyEvaluationResult<TOutput?> { Result = default };

        return result[0];
    }

    public PolicyEvaluationResult<TOutput> Evaluate<TInput, TOutput>(TInput input, string? entrypoint = null)
        where TOutput : notnull
    {
        var result = EvalInternal<TInput, PolicyEvaluationResult<TOutput>[]>(input, entrypoint);

        if (result == null || result.Length == 0)
            throw new OpaEvaluationException("Policy evaluator returned empty result");

        return result[0];
    }

    public string EvaluateRaw(ReadOnlySpan<char> inputJson, string? entrypoint = null)
    {
        var resultPtr = EvalInternal(inputJson, entrypoint);
        return _memory.ReadNullTerminatedString(resultPtr);
    }

    private TOutput? EvalInternal<TInput, TOutput>(TInput input, string? entrypoint = null)
    {
        var s = JsonSerializer.Serialize(input, JsonOptions);
        var jsonAdr = EvalInternal(s, entrypoint);

        if (jsonAdr == 0)
            return default;

        return _memory.ReadNullTerminatedJson<TOutput>(jsonAdr, JsonOptions);
    }

    private nint EvalInternal(ReadOnlySpan<char> inputJson, string? entrypoint = null)
    {
        try
        {
            return _abi.Eval(inputJson, entrypoint);
        }
        catch (WasmtimeException ex)
        {
            if (ex.InnerException is OpaEvaluationAbortedException)
                throw ex.InnerException;

            if (ex.InnerException is OpaBuiltinException)
                throw new OpaEvaluationException("Builtin failed", ex.InnerException);

            throw new OpaEvaluationException("Evaluation failed", ex);
        }
        finally
        {
            _importsAbi.Reset();
        }
    }

    public void Dispose()
    {
        _abi.Dispose();
        _module.Dispose();
        _store.Dispose();
        _linker.Dispose();
        _engine.Dispose();
    }
}