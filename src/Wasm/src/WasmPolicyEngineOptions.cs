﻿using System.Text.Encodings.Web;

using OpaDotNet.Wasm.Rego;

namespace OpaDotNet.Wasm;

/// <summary>
/// Contains members that affect OPA policy engine configuration.
/// </summary>
[PublicAPI]
public class WasmPolicyEngineOptions
{
    /// <summary>
    /// Default engine options.
    /// </summary>
    public static WasmPolicyEngineOptions Default { get; } = new();

    /// <summary>
    /// Creates default engine options.
    /// </summary>
    /// <param name="options">JSON serialization options.</param>
    /// <returns>Engine options.</returns>
    public static WasmPolicyEngineOptions DefaultWithJsonOptions(Action<JsonSerializerOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var result = new WasmPolicyEngineOptions();
        options(result.SerializationOptions);
        return result;
    }

    private readonly JsonSerializerOptions _jsonSerializationOptions = new()
    {
        Converters = { RegoSetJsonConverterFactory.Instance },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Minimal number of 64k pages available for WASM engine.
    /// </summary>
    public long MinMemoryPages { get; init; } = 2;

    /// <summary>
    /// Maximum number of 64k pages available for WASM engine.
    /// </summary>
    public long? MaxMemoryPages { get; init; }

    /// <summary>
    /// Max ABI versions to use.
    /// Can be useful for cases when you want evaluator to use lower ABI version than policy supports.
    /// </summary>
    public Version? MaxAbiVersion { get; init; }

    /// <summary>
    /// Directory used to keep unpacked policies. If <c>null</c> policies will be kept in memory.
    /// </summary>
    /// <remarks>
    /// Directory must exist and requires write permissions.
    /// </remarks>
    public string? CachePath { get; init; }

    /// <summary>
    /// If <c>true</c> errors in built-in functions will be threaded as exceptions that halt policy evaluation.
    /// </summary>
    public bool StrictBuiltinErrors { get; init; }

    /// <summary>
    /// OPA bundle signature validation options.
    /// </summary>
    public SignatureValidationOptions SignatureValidation { get; init; } = new();

    // /// <summary>
    // /// List of custom built-ins that were registered.
    // /// </summary>
    // public List<Func<IOpaCustomBuiltins>> CustomBuiltins { get; } = [];

    /// <summary>
    /// JSON serialization options.
    /// </summary>
    /// <exception cref="ArgumentNullException">Value is <c>null</c>.</exception>
    public JsonSerializerOptions SerializationOptions
    {
        get => _jsonSerializationOptions;
        init
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            _jsonSerializationOptions = new(value)
            {
                Converters = { RegoSetJsonConverterFactory.Instance },
            };
        }
    }
}