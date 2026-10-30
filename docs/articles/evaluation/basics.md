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

You can customize `IOpaEvaluator` behavior by providing [WasmPolicyEngineOptions](xref:OpaDotNet.Wasm.WasmPolicyEngineOptions).

## Set optional external data

If policy does not use external data or policy bundle already contains `data.json` file you can skip this step.

You can initialize external data with any of `IOpaEvaluator.SetData*` methods.

> [!NOTE]
> You can call `IOpaEvaluator.SetData*` if you need external data changed between policy evaluations.

You can reset `IOpaEvaluator` instance to "initial state" (aka no external data) by calling `IOpaEvaluator.Reset`.

## Evaluate policy with optional input

Once `IOpaEvaluator` instance created and external data have been initialized you can issue policy query evaluations.
You can provide optional input and optional entrypoint (`package` or `package/rule`). If entrypoint is not provided, default entrypoint is used.
OpaDotNet provides several methods:

- `IOpaEvaluator.EvaluatePredicate`. Useful when policy result is `true` or `false`.
- `IOpaEvaluator.Evaluate`. Useful when policy returns complex object.
- `IOpaEvaluator.EvaluateRaw`. Useful when you want control JSON serialization/deserialization manually.

> [!NOTE]
> You can use the same `IOpaEvaluator` instance to evaluate multiple consecutive policy queries as long as it's done from the single thread.
