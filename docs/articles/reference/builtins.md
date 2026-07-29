# Advanced Built-ins

This page covers implementation details for [custom built-in functions](~/articles/evaluation/bultins.md) implemented
via `IOpaCustomBuiltins` and `[OpaCustomBuiltin]`: how arguments are bound, what a built-in may return, how errors
propagate back into policy evaluation, and how to write built-ins whose results stay stable within a single
evaluation.

## Arguments

A method marked with `[OpaCustomBuiltin("name")]` may declare up to four positional parameters, matching the
arguments OPA passes to the built-in. It may optionally declare one additional, trailing parameter of type
`JsonSerializerOptions` or `IOpaCustomBuiltinsContext` — not both — to access evaluation-scoped state:

> [!IMPORTANT]
> Built-in arguments must be JSON-serializable.

```csharp
public class MyBuiltins : IOpaCustomBuiltins
{
    public void Reset()
    {
    }

    // Two positional arguments, no extra parameter.
    [OpaCustomBuiltin("my.concat")]
    public static string Concat(string a, string b) => a + b;

    // Trailing IOpaCustomBuiltinsContext gives access to the cancellation token
    // and the JsonSerializerOptions used for the current evaluation.
    [OpaCustomBuiltin("my.lookup")]
    public static async Task<string> Lookup(string key, IOpaCustomBuiltinsContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        return await LookupValueAsync(key, context.JsonSerializerOptions);
    }
}
```

Methods can be `static` or instance methods; both are supported identically. Prefer instance methods when a built-in
needs to carry state across calls (combined with `Reset()`, see [below](#implementing-deterministic-functions)) —
otherwise `static` avoids an unnecessary allocation per resolved built-in.

> [!NOTE]
> Generic built-in methods and methods with more than four positional arguments (plus the optional trailing
> parameter) are not supported.

## Returning results

A `[OpaCustomBuiltin]` method may return:

- Any JSON-serializable value — serialized back to Rego.
- `void` or `Task` — for built-ins that are called only for their side effect or to validate input; a successful
  call resolves to an empty object (`{}`) on the Rego side, and a thrown exception makes the call fail (see
  [Handling exceptions](#handling-exceptions) below).
- `Task<T>` — for asynchronous built-ins; awaited before the result is passed back into the evaluation.

```csharp
[OpaCustomBuiltin("my.validate")]
public static void Validate(string input)
{
    if (!IsValid(input))
        throw new ArgumentException($"invalid input: {input}");
}
```

> [!IMPORTANT]
> `ValueTask` and `ValueTask<T>` return types are not supported. Return `Task` or
> `Task<T>` instead.

> [!IMPORTANT]
> Due to specifics of `wasmtime-dotnet` runtime async built-ins will be forced to complete synchronously (aka `Task.Wait(...)`) before returning results back to OPA.

## Handling exceptions

By default (`WasmPolicyEngineOptions.StrictBuiltinErrors = false`, matching native OPA's default), an exception
thrown from a built-in is swallowed and the built-in call — and, transitively, whatever rule depends on it — becomes
`undefined`. Other, independent rules in the same query are unaffected.

Set `StrictBuiltinErrors = true` to make built-in errors fatal instead: any exception is wrapped and re-thrown.

```csharp
var opts = new WasmPolicyEngineOptions
{
    StrictBuiltinErrors = true,
};
```

Two exception types are handled specially regardless of `StrictBuiltinErrors`:

- `NotImplementedException` always aborts evaluation — throw it to signal "this built-in is intentionally
  unimplemented," which should never be silently treated as `undefined`.
- `OperationCanceledException` (for example from `context.CancellationToken.ThrowIfCancellationRequested()`) is
  translated into a timeout-flavored evaluation error rather than a generic built-in error.

## Implementing deterministic functions

Rego expects a built-in to behave deterministically *within a single evaluation*: repeated calls with the same
arguments during one `Evaluate` should return the same value, the way `time.now_ns()` returns a single frozen
timestamp for the whole query rather than drifting between calls. OpaDotNet resets all built-in state — including any
memoized values — after each evaluation completes, so caching is naturally scoped to one query.

For `[OpaCustomBuiltin]` methods, set `Memorize = true` to get this for free: the arguments are hashed, and the
result of the first call for a given argument set is cached and reused for subsequent calls with the same arguments
during the same evaluation.

```csharp
[OpaCustomBuiltin("my.currentBatchId", Memorize = true)]
public static Guid CurrentBatchId() => Guid.NewGuid();
```

If you override `Reset()` on a custom `IOpaCustomBuiltins` implementation, call the base implementation (or otherwise
clear your own caches) so memoized values don't leak into the next evaluation.
