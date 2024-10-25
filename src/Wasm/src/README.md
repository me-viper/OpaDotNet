# Open Policy Agent (OPA) WebAssembly dotnet core SDK

This is SDK for using WebAssembly (wasm) compiled [Open Policy Agent](https://www.openpolicyagent.org/) policies
with dotnet core.

Initial implementation was based
on [Open Policy Agent WebAssemby NPM Module](https://github.com/open-policy-agent/npm-opa-wasm)

For more information check out [the guide](https://me-viper.github.io/OpaDotNet/).

## Key Features

* Fast in-process OPA policies evaluation.
* Full ABI [support](https://andrii-kurochka.gitbook.io/opadotnet.wasm/overview/opa-compatibility/abi).
* Additional OPA [built-ins](https://andrii-kurochka.gitbook.io/opadotnet.wasm/overview/opa-compatibility/builtins).
* [Compilation](https://github.com/me-viper/OpaDotNet.Compilation).
* AspDotNet Core [integration](https://github.com/me-viper/OpaDotNet.Extensions).

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

const string data = "{ \"world\": \"world\" }";

using var engine = OpaEvaluatorFactory.CreateFromWasm(
    File.OpenRead("policy.wasm")
    );

engine.SetDataFromRawJson(data);

```

### Evaluate policy

`IOpaEvaluator` has several APIs for policy evaluation:

* `EvaluatePredicate` - Evaluates named policy with specified input. Response interpreted as simple `true`/`false`
  result.
* `Evaluate` - Evaluates named policy with specified input.
* `EvaluateRaw` - Evaluates named policy with specified raw JSON input.

```csharp
var policyResult = engine.EvaluatePredicate(inp);
```

### Check result

```csharp
if (policyResult.Result)
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

hello if {
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
The result will be an OPA bundle with the `policy.wasm` binary included. See [./samples](./samples) for a more
comprehensive example.

See `opa build --help` for more details.

### With OpaDotNet.Compilation

You can use SDK to do compilation for you. For more information
see [OpaDotNet.Compilation](https://github.com/me-viper/OpaDotNet/tree/main/src/Compilation).

#### OpaDotNet.Compilation.Cli

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
