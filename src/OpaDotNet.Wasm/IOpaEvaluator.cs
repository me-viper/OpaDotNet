using JetBrains.Annotations;

namespace OpaDotNet.Wasm;

[PublicAPI]
public interface IOpaEvaluator : IDisposable
{
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

    void SetDataFromRawJson(ReadOnlySpan<char> dataJson);
    
    void SetDataFromStream(Stream? utf8Json);

    void SetData<T>(T? data) where T : class;
    
    void Reset();
}