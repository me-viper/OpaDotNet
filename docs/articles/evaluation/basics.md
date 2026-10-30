# Policy Evaluation

OPA policy module evaluation requires the following steps:

1. Obtain compiled policy WASM binary or policy bundle.
2. Initialize evaluator instance.
3. Set optional external data.
4. Evaluate policy with optional input.

## Obtain compiled policy WASM binary or policy bundle

> [!IMPORTANT]
> If you are using precompiled policy bundle it should target WASM and contain `policy.wasm` file.

## Initialize evaluator instance

There are several ways to initialize evaluator instance:

- Static `Create` methods:
  - [OpaBundleEvaluatorFactory.Create](xref:OpaDotNet.Wasm.OpaBundleEvaluatorFactory). Creates evaluator instance from policy bundle.
  - [OpaWasmEvaluatorFactory.Create](xref:OpaDotNet.Wasm.OpaWasmEvaluatorFactory). Creates evaluator instance from policy WASM binary.
- Factory instances:
  - [OpaBundleEvaluatorFactory](xref:OpaDotNet.Wasm.OpaBundleEvaluatorFactory). Creates evaluator instances from policy bundle.
  - [OpaWasmEvaluatorFactory](xref:OpaDotNet.Wasm.OpaWasmEvaluatorFactory). Creates evaluator instances from policy WASM binary.

In general you would use:

- Static `Create` methods if you need to create single instance of `IOpaEvaluator` (e.g. cli tools, scripts etc.)
- Factory instance (`new OpaBundleEvaluatorFactory(...)` or `new OpaWasmEvaluatorFactory(...)`) and its `Create()`
  method if you need to create multiple `IOpaEvaluator` instances from the same policy bundle or WASM binary (e.g.
  AspNetCore applications/services). Dispose the factory when you no longer need it.

> [!CAUTION]
> `IOpaEvaluator` instances are NOT thread safe.

You can customize `IOpaEvaluator` behavior by providing [WasmPolicyEngineOptions](xref:OpaDotNet.Wasm.WasmPolicyEngineOptions),
see [Engine Options](options.md).

## Set optional external data

If policy does not use external data or policy bundle already contains `data.json` file you can skip this step.

[!code-csharp[](~/snippets/Evaluation.cs#EvaluationUsings)]

[!code-csharp[](~/snippets/Evaluation.cs#EvaluationSetData)]

- `SetData<T>` serializes the object with
  [SerializationOptions](options.md#json-serialization).
- `SetDataFromRawJson` passes a JSON string as is.
- `SetDataFromStream` reads UTF-8 JSON from a seekable stream (it uses `Stream.Length`). The evaluator doesn't dispose
  the stream.

Each call replaces the whole external data document, including data loaded from the bundle's `data.json`. You can call
them between evaluations to change the data.

`IOpaEvaluator.Reset` returns the evaluator to its initial state with no external data, including no bundle data:

[!code-csharp[](~/snippets/Evaluation.cs#EvaluationReset)]

On ABI 1.3+ policies you can also change part of the data without replacing all of it through
[IUpdateDataFeature](xref:OpaDotNet.Wasm.Features.IUpdateDataFeature), available from
`IOpaEvaluator.TryGetFeature`.

## Evaluate policy with optional input

Once `IOpaEvaluator` instance created and external data have been initialized you can issue policy query evaluations.
You can provide optional input and optional entrypoint (`package/rule`, as passed to the compiler). If entrypoint is
not provided, the first entrypoint the policy was compiled with is used.

Input and results are serialized with [SerializationOptions](options.md#json-serialization). The example below uses
camel case naming, so `UserInfo.Name` is matched with `name` in the policy output:

[!code-rego[](~/snippets/evaluation/policy.rego)]

[!code-csharp[](~/snippets/Evaluation.cs#EvaluationOutput)]

[!code-csharp[](~/snippets/Evaluation.cs#EvaluationMethods)]

| Method                               | Use when                                        |
|--------------------------------------|-------------------------------------------------|
| `EvaluatePredicate<TInput>`          | The rule is a boolean decision.                 |
| `Evaluate<TInput, TOutput>`          | The rule returns a value and must be defined.   |
| `EvaluateOrDefault<TInput, TOutput>` | The rule returns a value and may be undefined.  |
| `EvaluateRaw`                        | You want to handle JSON serialization yourself. |

The typed methods return [PolicyEvaluationResult&lt;T&gt;](xref:OpaDotNet.Wasm.PolicyEvaluationResult`1), with the
rule value in its `Result` property. It mirrors an element of OPA's result set, `{"result": ...}`. `EvaluateRaw`
returns the whole result set as a JSON string, for example `[{"result":true}]`, or `[]` if the rule is undefined.

See [Error Handling](errors.md) for what these methods return for undefined results and which exceptions they throw.

> [!NOTE]
> You can use the same `IOpaEvaluator` instance to evaluate multiple consecutive policy queries as long as it's done from the single thread.
