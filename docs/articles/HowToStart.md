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

More samples [here](https://github.com/me-viper/OpaDotNet/tree/main/src/Extensions.AspNetCore/samples)

## Writing policy

See [writing policy](https://www.openpolicyagent.org/docs/latest/how-do-i-write-policies/)

### Compiling policy

You have several options to compile rego policy into wasm module:

Consider `example.rego` file with the following policy:

[!code-rego[](~/snippets/quickstart/example.rego)]

### Manually

Either use the Compile REST API or opa build CLI tool:

```sh
opa build -t wasm -e example/hello example.rego
```

Which is compiling the `example.rego` policy file.
The result will be an OPA bundle with the `policy.wasm` binary included. See [samples](https://github.com/me-viper/OpaDotNet/tree/main/src/Extensions.AspNetCore/samples) for a more comprehensive example.

See `opa build --help` for more details.

### With OpaDotNet.Compilation

You can use SDK to do compilation for you. For more information see [Compilation](~/articles/compilation/compilation.md).

#### OpaDotNet.Compilation.Cli

> [!IMPORTANT]
> You will need `opa` cli tool to be in your PATH or provide full path in `RegoCliCompilerOptions`.

```bash
dotnet add package OpaDotNet.Compilation.Cli
```

[!code-csharp[](~/snippets/QuickStart.cs#CompilationUsings)]

[!code-csharp[](~/snippets/QuickStart.cs#QuickStartCompilation)]

#### OpaDotNet.Compilation.Interop

```bash
dotnet add package OpaDotNet.Compilation.Interop
```

[!code-csharp[](~/snippets/QuickStart.cs#CompilationInteropUsings)]

[!code-csharp[](~/snippets/QuickStart.cs#QuickStartCompilationInterop)]
