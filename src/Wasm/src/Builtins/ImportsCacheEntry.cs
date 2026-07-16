namespace OpaDotNet.Wasm.Builtins;

internal record ImportsCacheEntry(
    Type Type,
    Func<IOpaCustomBuiltins, BuiltinArg[], IOpaCustomBuiltinsContext, Task<object?>> Import,
    CustomBuiltinInfo Attributes
    );