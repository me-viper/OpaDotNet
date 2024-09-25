namespace OpaDotNet.Wasm.Builtins;

internal record ImportsCacheEntry(
    Type Type,
    Func<IOpaCustomBuiltins, BuiltinArg[], object?> Import,
    OpaCustomBuiltinAttribute Attributes
    );