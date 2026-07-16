namespace OpaDotNet.Wasm.Builtins;

/// <summary>
/// Context available to custom built-in function implementations.
/// </summary>
public interface IOpaCustomBuiltinsContext
{
    /// <summary>
    /// JSON serialization options.
    /// </summary>
    JsonSerializerOptions JsonSerializerOptions { get; }

    /// <summary>
    /// Cancellation token.
    /// </summary>
    CancellationToken CancellationToken { get; }
}

internal class OpaCustomBuiltinsContext : IOpaCustomBuiltinsContext
{
    public required JsonSerializerOptions JsonSerializerOptions { get; init; }

    public required CancellationToken CancellationToken { get; init; }
}