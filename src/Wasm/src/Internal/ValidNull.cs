namespace OpaDotNet.Wasm.Internal;

internal sealed class ValidNull
{
    public static ValidNull Instance { get; } = new();

    private ValidNull()
    {
    }
}