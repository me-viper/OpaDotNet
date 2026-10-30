# Migration guide

## From v2.x to 3.x

### Evaluator factories

Static `OpaEvaluatorFactory` class has been removed. Use static `Create` methods of `OpaBundleEvaluatorFactory` or
`OpaWasmEvaluatorFactory` instead (see [Policy Evaluation](~/articles/evaluation/basics.md)):

```diff
-using var engine = OpaEvaluatorFactory.CreateFromBundle(policyStream);
+using var engine = OpaBundleEvaluatorFactory.Create(policyStream);
```

```diff
-using var engine = OpaEvaluatorFactory.CreateFromWasm(policyStream);
+using var engine = OpaWasmEvaluatorFactory.Create(policyStream);
```

### Custom built-ins

`DefaultBuiltinsFactory` has been removed. The recommended way to implement custom built-ins is `IOpaCustomBuiltins`
with methods marked with `[OpaCustomBuiltin("name")]` instead of extending `DefaultOpaImportsAbi` and overriding
`Func(...)` overloads. Register the implementation via `WasmPolicyEngineOptions`:

```diff
-public class CustomBuiltinsSample : DefaultOpaImportsAbi
-{
-    public override object? Func(BuiltinContext context, BuiltinArg arg1)
-    {
-        if (string.Equals("custom.oneArgBuiltin", context.FunctionName, StringComparison.Ordinal))
-            return $"hello {arg1.AsOrNull<string>()}";
-
-        return base.Func(context, arg1);
-    }
-}
+public class OpaCustomBuiltins : IOpaCustomBuiltins
+{
+    [OpaCustomBuiltin("custom.oneArgBuiltin")]
+    public static string OneArgBuiltin(string arg1) => $"hello {arg1}";
+}
```

```diff
-using var factory = new OpaBundleEvaluatorFactory(
-    policy,
-    null,
-    new DefaultBuiltinsFactory(() => new CustomBuiltinsSample())
-    );
+var opts = new WasmPolicyEngineOptions();
+opts.ConfigureBuiltins(p => p.CustomBuiltins.Add(new OpaCustomBuiltins()));
+
+using var factory = new OpaBundleEvaluatorFactory(policy, opts);
```

If you want to keep existing `DefaultOpaImportsAbi` subclass, assign it to `WasmBuiltinsOptions.DefaultBuiltins`
instead of using `DefaultBuiltinsFactory`:

```diff
-using var factory = new OpaBundleEvaluatorFactory(
-    policy,
-    null,
-    new DefaultBuiltinsFactory(() => new CustomBuiltinsSample())
-    );
+var opts = new WasmPolicyEngineOptions();
+opts.ConfigureBuiltins(p => p.DefaultBuiltins = new CustomBuiltinsSample());
+
+using var factory = new OpaBundleEvaluatorFactory(policy, opts);
```

See [Custom Built-in Functions](~/articles/evaluation/builtins.md) for the complete sample.

### Compilation packages

`OpaDotNet.Compilation.*` packages have moved into the [OpaDotNet](https://github.com/me-viper/OpaDotNet) repository.
Package names and namespaces are unchanged.

## From v1.x to 2.x

In `2.x` release compilation related logic have been moved out `OpaDotNet.Wasm` into separate assembly `OpaDotNet.Compilation.Cli`.
If you were using types from `OpaDotNet.Wasm.Compilation` namespace you will need to do the following changes:

```bash
dotnet add package OpaDotNet.Compilation.Cli
```

```diff
-using OpaDotNet.Wasm.Compilation
+using OpaDotNet.Compilation.Cli;

var compiler = new RegoCliCompiler();
var policyStream = await compiler.CompileFile("example.rego", new[] { "example/hello" });

// Use compiled policy.
using var engine = OpaEvaluatorFactory.CreateFromBundle(policyStream);
```

You might also want to check out new [alternative](~/articles/compilation/interop.md) to the Cli compiler.
