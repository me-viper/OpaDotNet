using Wasmtime;

namespace OpaDotNet.Wasm.Internal;

internal interface IAbiInitializer<out T>
{
    static abstract T Initialize(Instance instance);
}