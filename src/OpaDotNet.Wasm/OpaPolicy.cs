namespace OpaDotNet.Wasm;

internal record OpaPolicy(Stream Policy, Stream? Data = null) : IDisposable
{
    public void Dispose()
    {
        Policy.Dispose();
        Data?.Dispose();
    }
}