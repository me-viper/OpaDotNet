---
layout: default
---

# Open Policy Agent (OPA) WebAssembly dotnet core SDK

This is SDK for using WebAssembly (wasm) compiled [Open Policy Agent](https://www.openpolicyagent.org/) Rego policies with dotnet core.

For more information check out [the guide](https://andrii-kurochka.gitbook.io/opadotnet.wasm/).

## Getting Started

### Install nuget package

```sh
dotnet add package OpaDotNet.Wasm
```

## Usage

To evaluate OPA policy you need to:

### Load compiled policy

```csharp
using using OpaDotNet.Wasm;

var factory = new OpaEvaluatorFactory();

const string data = "{ \"world\": \"world\" }";

using var engine = factory.CreateFromWasm(
    File.OpenRead("policy.wasm")
    );

engine.SetDataFromRawJson(data);

```

### Evaluate policy

`IOpaEvaluator` has several APIs for policy evaluation:

* `EvaluatePredicate` - Evaluates named policy with specified input. Response interpreted as simple `true`/`false` result.
* `Evaluate` - Evaluates named policy with specified input.
* `EvaluateRaw` - Evaluates named policy with specified raw JSON input.

```csharp
var policyResult = engine.EvaluatePredicate(inp);
```

### Check result

```csharp
if (policyResult)
{
    // We've been authorized.
}
else
{
    // Can't do that.
}
```

## Writing policy

See [writing policy](https://www.openpolicyagent.org/docs/latest/how-do-i-write-policies/)

### Compiling policy

You have several options to compile rego policy into wasm module:

```rego
package example

default hello = false

hello {
    x := input.message
    x == data.world
}
```

### Manually

Either use the Compile REST API or opa build CLI tool.

For example, with OPA v0.20.5+:

```sh
opa build -t wasm -e example/hello example.rego
```

Which is compiling the `example.rego` policy file.
The result will be an OPA bundle with the `policy.wasm` binary included. See [./samples](./samples) for a more comprehensive example.

See `opa build --help` for more details.

### With OpaDotNet.Wasm.Compilation

You can use SDK to do compilation for you.

**Important**. You will need `opa` cli tool to be in your PATH or provide full path in `RegoCliCompilerOptions`.

```csharp
using OpaDotNet.Wasm;
using OpaDotNet.Wasm.Compilation;

var options = new OptionsWrapper<RegoCliCompilerOptions>(new RegoCliCompilerOptions());
var compiler = new RegoCliCompiler(options);
var policyStream = await compiler.CompileFile("example.rego", new[] { "example/hello" });

// Use compiled policy.
var factory = new OpaEvaluatorFactory();

using var engine = factory.CreateFromBundle(policyStream);
```