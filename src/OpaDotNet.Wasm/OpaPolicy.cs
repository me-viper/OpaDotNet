namespace OpaDotNet.Wasm;

internal record OpaPolicy(ReadOnlyMemory<byte> Policy, ReadOnlyMemory<byte>? Data = null);