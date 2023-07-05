﻿namespace OpaDotNet.Wasm.Internal;

internal interface IWasmPolicyEngine : IDisposable
{
    Version AbiVersion { get; }

    IReadOnlyDictionary<int, string> Builtins { get; }

    nint WriteJsonString(ReadOnlySpan<char> data);

    nint WriteValueString(ReadOnlySpan<char> data);

    nint WriteValue<T>(T? data);

    nint WriteJson<T>(T? data);

    string ReadValueString(nint ptr);

    string ReadJsonString(nint ptr);

    T ReadJson<T>(nint ptr);

    void SetData(ReadOnlySpan<char> dataJson);

    nint Eval(ReadOnlySpan<char> inputJson, string? entrypoint = null);

    void Reset();

    void SetData(Stream? data);

    string DumpData();
}