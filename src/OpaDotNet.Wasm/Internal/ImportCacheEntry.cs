namespace OpaDotNet.Wasm.Internal;

internal record ImportCacheEntry(Type Type, Func<IOpaCustomBuiltins, BuiltinArg[], object?> Import);