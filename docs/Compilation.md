# Policy Compilation

To evaluate policy modules they need to be compiled into WASM.

You can compile them manually with `opa build -t wasm ...` command or ask OpaDotNet do that for you.

**Important**. Internally `RegoCliCompiler` uses `opa` CLI so you need to ensure it is in your `PATH` environment variable or you provided full path in `RegoCliCompilerOptions.OpaToolPath`.

## Compiling single policy file

```csharp
var compiler = new RegoCliCompiler();

var policy = await compiler.CompileFile(
  // Policy source file.
  "policy.rego",
  // Entrypoints (same you would pass for -e parameter for opa build).
  new[] { "example/allow" }
  );

// RegoCliCompiler will always produce bundle.
using var engine = OpaEvaluatorFactory.CreateFromBundle(policy);

...
```

## Compiling bundle

Create [bundle directory](https://www.openpolicyagent.org/docs/latest/management-bundles/)

```csharp
var compiler = new RegoCliCompiler();

var policy = await compiler.CompileBundle(
  // Directory with bundle sources.
  "bundleDirectory",
  // Entrypoints (same you would pass for -e parameter for opa build).
  new[] { "example/allow" }
  );

// RegoCliCompiler will always produce bundle.
using var engine = OpaEvaluatorFactory.CreateFromBundle(policy);

...
```
