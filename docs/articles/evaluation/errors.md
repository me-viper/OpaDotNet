# Error Handling

## Exception types

All exceptions specific to OpaDotNet are derived from
[OpaRuntimeException](xref:OpaDotNet.Wasm.OpaRuntimeException):

```text
OpaRuntimeException                         policy can't be loaded or initialized
├── OpaEvaluationException                  policy evaluation failed
├── ExportResolutionException               WASM module doesn't export a function the OPA ABI requires
└── BundleSignatureValidationException      bundle signature validation failed
    └── BundleChecksumValidationException   file hash doesn't match the signature
```

[!code-csharp[](~/snippets/Errors.cs#ErrorsUsings)]

## Loading policies

Evaluator factories and the static `Create` methods read, unpack and instantiate the policy straight away, so a broken
policy fails here rather than during evaluation.

| Cause                                                      | Exception                                                                  |
|------------------------------------------------------------|----------------------------------------------------------------------------|
| Bundle can't be unpacked, or doesn't contain `policy.wasm` | `OpaRuntimeException`                                                      |
| Bundle signature is missing (when required) or invalid     | `BundleSignatureValidationException`                                       |
| File in the bundle doesn't match its signed hash           | `BundleChecksumValidationException`                                        |
| WASM module doesn't expose the OPA ABI                     | `ExportResolutionException`, `OpaRuntimeException`                         |
| `CachePath` directory doesn't exist                        | `DirectoryNotFoundException`, wrapped in `OpaRuntimeException` for bundles |

A WASM module that can't be instantiated at all (e.g. one that isn't an OPA policy and has unrelated imports) fails
with Wasmtime's own `WasmtimeException`. See [Bundle Signature Validation](../compilation/signing.md) for details on
signature errors.

[!code-csharp[](~/snippets/Errors.cs#LoadPolicyErrors)]

## Evaluating policies

Any failure during an `Evaluate*` call is reported as
[OpaEvaluationException](xref:OpaDotNet.Wasm.OpaEvaluationException). Its message and `InnerException` tell you the
cause.

| Cause                                                                                 | Message / inner exception                   |
|---------------------------------------------------------------------------------------|---------------------------------------------|
| Entrypoint passed to `Evaluate*` isn't defined in the policy                          | `Unknown entrypoint ...`                    |
| `Evaluate<TInput, TOutput>` got an undefined result (see [below](#undefined-results)) | `Policy evaluator returned empty result`    |
| Evaluation exceeded [Timeout](options.md#timeout)                                     | `Evaluation timeout`, inner `TrapException` |
| Built-in observed the timeout through its cancellation token                          | inner `OperationCanceledException`          |
| Memory needed exceeds [MaxMemoryPages](options.md#memory)                             | `Evaluation failed`, or `Aborted: ...`      |
| Policy runtime error, e.g. conflicting values for a complete rule or function         | `Aborted: ...`                              |
| Built-in threw and `StrictBuiltinErrors` is `true`, or built-in isn't implemented     | the built-in's exception as inner exception |

Built-in errors are handled differently depending on `StrictBuiltinErrors`. By default they make the built-in call
undefined instead of failing the evaluation. See [Handling exceptions](../reference/builtins.md#handling-exceptions).

A failed evaluation doesn't break the evaluator: its state is reset after every call, so you can keep using it.

[!code-csharp[](~/snippets/Errors.cs#EvaluationErrors)]

> [!NOTE]
> Input serialization and result deserialization use `System.Text.Json` and aren't wrapped. A result that doesn't
> match `TOutput` throws `JsonException`. Check your [serialization options](options.md#json-serialization) if that
> happens.

## Undefined results

In Rego a rule whose conditions aren't met is
[undefined](https://www.openpolicyagent.org/docs/latest/policy-language/#rules) rather than `false` or `null`, unless
it has a `default` value. The `Evaluate*` methods handle this differently:

| Method                               | Undefined result                |
|--------------------------------------|---------------------------------|
| `EvaluatePredicate`                  | `Result` is `false`             |
| `EvaluateOrDefault<TInput, TOutput>` | `Result` is `default(TOutput)`  |
| `Evaluate<TInput, TOutput>`          | throws `OpaEvaluationException` |
| `EvaluateRaw`                        | returns `[]`                    |

[!code-csharp[](~/snippets/Errors.cs#UndefinedResults)]

> [!TIP]
> `EvaluatePredicate` can't tell `false` from undefined. That's usually what you want for an `allow` rule, which should
> fail closed. If you need to tell them apart, use `EvaluateOrDefault<TInput, bool?>` or add a `default` to the rule.
