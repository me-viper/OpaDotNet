# Migration guide

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

You might also want to check out new [alternative](https://github.com/me-viper/OpaDotNet.Compilation/tree/main/src/OpaDotNet.Compilation.Interop) to the Cli compiler.
