namespace OpaDotNet.Wasm.Builtins;

internal record ImportsCacheEntry(
    Type Type,
    Func<IOpaCustomBuiltins, BuiltinArg[], JsonSerializerOptions, object?> Import,
    OpaCustomBuiltinAttribute Attributes
    );