using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using OpaDotNet.Wasm.Features;

namespace OpaDotNet.Wasm;

/// <summary>
/// OPA policy evaluator.
/// </summary>
[PublicAPI]
public interface IOpaEvaluator : IDisposable
{
    /// <summary>
    /// ABI version used for evaluation.
    /// </summary>
    Version AbiVersion { get; }

    /// <summary>
    /// Evaluates named policy with specified input. Result interpreted as simple <c>true</c>/<c>false</c> response.   
    /// </summary>
    /// <param name="input">Policy input document</param>
    /// <param name="entrypoint">Policy decision to ask for</param>
    /// <returns>Policy evaluation result</returns>
    PolicyEvaluationResult<bool> EvaluatePredicate<TInput>(TInput input, string? entrypoint = null);

    /// <summary>
    /// Evaluates named policy with specified input.
    /// </summary>
    /// <param name="input">Policy input document</param>
    /// <param name="entrypoint">Policy decision to ask for</param>
    /// <returns>Policy evaluation result</returns>
    PolicyEvaluationResult<TOutput> Evaluate<TInput, TOutput>(TInput input, string? entrypoint = null)
        where TOutput : notnull;

    /// <summary>
    /// Evaluates named policy with specified raw JSON input.
    /// </summary>
    /// <param name="inputJson">Policy input document as JSON string</param>
    /// <param name="entrypoint">Policy decision to ask for</param>
    /// <returns>Policy evaluation result as JSON string</returns>
    string EvaluateRaw(ReadOnlySpan<char> inputJson, string? entrypoint = null);

    /// <summary>
    /// Sets external data.
    /// </summary>
    /// <param name="dataJson">External data JSON as raw string</param>
    void SetDataFromRawJson(ReadOnlySpan<char> dataJson);
    
    /// <summary>
    /// Sets external data.
    /// </summary>
    /// <param name="utf8Json">External data JSON as UTF-8 encoded stream</param>
    void SetDataFromStream(Stream? utf8Json);

    /// <summary>
    /// Sets external data.
    /// </summary>
    /// <param name="data">External data</param>
    void SetData<T>(T? data) where T : class;
    
    /// <summary>
    /// Resets evaluator to initial state. External data is removed.
    /// </summary>
    void Reset();

    /// <summary>
    /// Gets ABI version specific extensions.
    /// </summary>
    /// <param name="feature">ABI extension implementation</param>
    /// <returns><c>true</c> if extension is supported; otherwise <c>false</c></returns>
    bool TryGetFeature<TFeature>([MaybeNullWhen(false)] out TFeature feature)
        where TFeature : class, IOpaEvaluatorFeature;
}