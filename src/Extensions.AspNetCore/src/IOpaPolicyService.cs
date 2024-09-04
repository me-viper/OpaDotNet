using JetBrains.Annotations;

namespace OpaDotNet.Extensions.AspNetCore;

[PublicAPI]
public interface IOpaPolicyService
{
    /// <summary>
    /// Evaluates named policy with specified input. Result interpreted as simple <c>true</c>/<c>false</c> response.   
    /// </summary>
    /// <param name="input">Policy input document</param>
    /// <param name="entrypoint">Policy decision to ask for</param>
    /// <returns>Policy evaluation result</returns>
    bool EvaluatePredicate<TInput>(TInput input, string entrypoint);

    /// <summary>
    /// Evaluates named policy with specified input.
    /// </summary>
    /// <param name="input">Policy input document</param>
    /// <param name="entrypoint">Policy decision to ask for</param>
    /// <returns>Policy evaluation result</returns>
    TOutput Evaluate<TInput, TOutput>(TInput input, string entrypoint)
        where TOutput : notnull;

    /// <summary>
    /// Evaluates named policy with specified raw JSON input.
    /// </summary>
    /// <param name="inputJson">Policy input document as JSON string</param>
    /// <param name="entrypoint">Policy decision to ask for</param>
    /// <returns>Policy evaluation result as JSON string</returns>
    string EvaluateRaw(ReadOnlySpan<char> inputJson, string entrypoint);
}