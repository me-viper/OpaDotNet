﻿namespace OpaDotNet.Wasm;

/// <summary>
/// Exposes OPA built-ins implementation.
/// </summary>
public interface IOpaImportsAbi
{
    /// <summary>
    /// Called if an internal error occurs.
    /// </summary>
    /// <param name="message">Error message.</param>
    void Abort(string message) => throw new OpaEvaluationAbortedException("Aborted: " + message);

    /// <summary>
    /// Called to emit a message from the policy evaluation.
    /// </summary>
    /// <param name="message">Message.</param>
    void PrintLn(string message) => Print([message]);

    /// <summary>
    /// Called to emit a message from <c>print</c> statement.
    /// </summary>
    /// <param name="args"></param>
    void Print(IEnumerable<string> args);

    /// <summary>
    /// Called to dispatch the built-in function.
    /// </summary>
    /// <param name="context">Call context.</param>
    /// <returns>JSON serializable function result.</returns>
    object? Func(BuiltinContext context);

    /// <summary>
    /// Called to dispatch the built-in function.
    /// </summary>
    /// <param name="context">Call context.</param>
    /// <param name="arg1">Function argument.</param>
    /// <returns>JSON serializable function result.</returns>
    object? Func(BuiltinContext context, BuiltinArg arg1);

    /// <summary>
    /// Called to dispatch the built-in function.
    /// </summary>
    /// <param name="context">Call context.</param>
    /// <param name="arg1">Function argument.</param>
    /// <param name="arg2">Function argument.</param>
    /// <returns>JSON serializable function result.</returns>
    object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2);

    /// <summary>
    /// Called to dispatch the built-in function.
    /// </summary>
    /// <param name="context">Call context.</param>
    /// <param name="arg1">Function argument.</param>
    /// <param name="arg2">Function argument.</param>
    /// <param name="arg3">Function argument.</param>
    /// <returns>JSON serializable function result.</returns>
    object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3);

    /// <summary>
    /// Called to dispatch the built-in function.
    /// </summary>
    /// <param name="context">Call context.</param>
    /// <param name="arg1">Function argument.</param>
    /// <param name="arg2">Function argument.</param>
    /// <param name="arg3">Function argument.</param>
    /// <param name="arg4">Function argument.</param>
    /// <returns>JSON serializable function result.</returns>
    object? Func(BuiltinContext context, BuiltinArg arg1, BuiltinArg arg2, BuiltinArg arg3, BuiltinArg arg4);

    /// <summary>
    /// Resets built-ins between evaluations.
    /// </summary>
    void Reset();
}