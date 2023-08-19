# Getting Started

## Install nuget package

```sh
dotnet add package OpaDotNet.Wasm
```

## Usage

To evaluate OPA policy you need to:

### Add usings

[!code-csharp[](~/snippets/QuickStart.cs#Usings)]

### Load compiled policy

[!code-csharp[](~/snippets/QuickStart.cs#QuickStartLoad)]

### Evaluate policy

`IOpaEvaluator` has several APIs for policy evaluation:

* `EvaluatePredicate` - Evaluates named policy with specified input. Response interpreted as simple `true`/`false` result.
* `Evaluate` - Evaluates named policy with specified input.
* `EvaluateRaw` - Evaluates named policy with specified raw JSON input.

[!code-csharp[](~/snippets/QuickStart.cs#QuickStartEval)]

### Check the result

[!code-csharp[](~/snippets/QuickStart.cs#QuickStartCheck)]

More samples [here](https://github.com/me-viper/OpaDotNet/tree/main/samples)

## Writing policy

See [writing policy](https://www.openpolicyagent.org/docs/latest/how-do-i-write-policies/)

### Compiling policy

You have several options to compile rego policy into wasm module:

Consider `example.rego` file with the following policy:

[!code-rego[](~/snippets/quickstart/example.rego)]

### Manually

Either use the Compile REST API or opa build CLI tool.

For example, with OPA v0.20.5+:

```sh
opa build -t wasm -e example/hello example.rego
```

Which is compiling the `example.rego` policy file.
The result will be an OPA bundle with the `policy.wasm` binary included. See [samples](https://github.com/me-viper/OpaDotNet/tree/main/samples) for a more comprehensive example.

See `opa build --help` for more details.

### With OpaDotNet.Compilation

You can use SDK to do compilation for you. For more information see [OpaDotNet.Compilation](https://github.com/me-viper/OpaDotNet.Compilation).

#### OpaDotNet.Compilation.Cli

> [!IMPORTANT]
> You will need `opa` cli tool to be in your PATH or provide full path in `RegoCliCompilerOptions`.

```bash
dotnet add package OpaDotNet.Compilation.Cli
```

```csharp
using OpaDotNet.Wasm;
using OpaDotNet.Compilation.Cli;

var compiler = new RegoCliCompiler();
var policyStream = await compiler.CompileFile("example.rego", new[] { "example/hello" });

// Use compiled policy.
using var engine = OpaEvaluatorFactory.CreateFromBundle(policyStream);
```

#### OpaDotNet.Compilation.Interop

```bash
dotnet add package OpaDotNet.Compilation.Interop
```

```csharp
using OpaDotNet.Wasm;
using OpaDotNet.Compilation.Interop;

var compiler = new RegoInteropCompiler();
var policyStream = await compiler.CompileFile("example.rego", new[] { "example/hello" });

// Use compiled policy.
using var engine = OpaEvaluatorFactory.CreateFromBundle(policyStream);
```
