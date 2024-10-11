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
    ValueTask<bool> EvaluatePredicate<TInput>(TInput input, string entrypoint)
        => EvaluatePredicate(input, entrypoint, CancellationToken.None);

    /// <summary>
    /// Evaluates named policy with specified input. Result interpreted as simple <c>true</c>/<c>false</c> response.
    /// </summary>
    /// <param name="input">Policy input document</param>
    /// <param name="entrypoint">Policy decision to ask for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Policy evaluation result</returns>
    ValueTask<bool> EvaluatePredicate<TInput>(TInput input, string entrypoint, CancellationToken cancellationToken);

    /// <summary>
    /// Evaluates named policy with specified input.
    /// </summary>
    /// <param name="input">Policy input document</param>
    /// <param name="entrypoint">Policy decision to ask for</param>
    /// <returns>Policy evaluation result</returns>
    ValueTask<TOutput> Evaluate<TInput, TOutput>(TInput input, string entrypoint)
        where TOutput : notnull => Evaluate<TInput, TOutput>(input, entrypoint, CancellationToken.None);

    /// <summary>
    /// Evaluates named policy with specified input.
    /// </summary>
    /// <param name="input">Policy input document</param>
    /// <param name="entrypoint">Policy decision to ask for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Policy evaluation result</returns>
    ValueTask<TOutput> Evaluate<TInput, TOutput>(TInput input, string entrypoint, CancellationToken cancellationToken)
        where TOutput : notnull;

    /// <summary>
    /// Evaluates named policy with specified raw JSON input.
    /// </summary>
    /// <param name="inputJson">Policy input document as JSON string</param>
    /// <param name="entrypoint">Policy decision to ask for</param>
    /// <returns>Policy evaluation result as JSON string</returns>
    ValueTask<string> EvaluateRaw(ReadOnlyMemory<char> inputJson, string entrypoint)
        => EvaluateRaw(inputJson, entrypoint, CancellationToken.None);

    /// <summary>
    /// Evaluates named policy with specified raw JSON input.
    /// </summary>
    /// <param name="inputJson">Policy input document as JSON string</param>
    /// <param name="entrypoint">Policy decision to ask for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Policy evaluation result as JSON string</returns>
    ValueTask<string> EvaluateRaw(ReadOnlyMemory<char> inputJson, string entrypoint, CancellationToken cancellationToken);
}