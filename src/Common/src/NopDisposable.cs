namespace OpaDotNet.Common;

internal sealed class NopDisposable : IDisposable
{
    public void Dispose()
    {
    }
}