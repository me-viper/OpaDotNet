# Policy Evaluation

OPA policy module evaluation requires the following steps:

1. Create evaluator instance.
2. Set optional external data.
3. Evaluate policy with optional input.

## 1. Create evaluator instance

Use `OpaEvaluatorFactory` to create evaluator instance.

Same `IOpaEvaluator` instance can be used to evaluate policy module multiple times.

**Important**. `IOpaEvaluator` instances are **NOT** thread-safe and should not be used from different threads.

### Create evaluator from OPA bundle

**Important**. If you compiled bundle manually it should target WASM.

```csharp
using var engine = OpaEvaluatorFactory.CreateFromBundle(File.OpenRead("bundle.tar.gz"));
```

### Create evaluator from compiled WASM binary

```csharp
using var engine = OpaEvaluatorFactory.CreateFromWasm(File.OpenRead("policy.wasm"));
```

## 2. Set optional external data

If your policy module does not require external data or OPA bundle already contains everything you need this step is optional.

```csharp
var data = "{\"world\":\"world\"}";
engine.SetDataFromRawJson(data);
```

## 3. Evaluate

```csharp
var policyResult1 = engine.EvaluatePredicate(new { Message = "Hello" });

if (policyResult1.Result)
{
    // We've been authorized.
}
else
{
    // Can't do that.
}

// Reusing evaluator same instance. Evaluating policy module with no input.
var policyResult2 = engine.EvaluatePredicate((object?)null);

if (policyResult2.Result)
{
    // We've been authorized.
}
else
{
    // Can't do that.
}
```
