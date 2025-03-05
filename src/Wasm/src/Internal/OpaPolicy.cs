namespace OpaDotNet.Wasm.Internal;

internal record OpaPolicy(ReadOnlyMemory<byte> Policy, ReadOnlyMemory<byte> Data);