namespace OpaDotNet.Wasm.Internal;

internal record ImportsCacheEntry(Type Type, Func<IOpaCustomBuiltins, BuiltinArg[], object?> Import);