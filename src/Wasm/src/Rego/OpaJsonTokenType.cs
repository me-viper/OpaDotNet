namespace OpaDotNet.Wasm.Rego;

internal enum OpaJsonTokenType
{
    Eof = 0,
    Null,
    Value,
    String,
    ObjectStart,
    ObjectEnd,
    ArrayStart,
    ArrayEnd,
    PropertyName,
    SetStart,
    SetEnd,
    EmptySet,
    Colon,
    Coma,
}