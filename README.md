﻿[![CI](https://github.com/me-viper/OpaDotNet/workflows/CI/badge.svg)](https://github.com/me-viper/OpaDotNet)
[![Coverage Status](https://coveralls.io/repos/github/me-viper/OpaDotNet/badge.svg)](https://coveralls.io/github/me-viper/OpaDotNet)

# Open Policy Agent (OPA) WebAssembly dotnet core SDK

This is SDK for using WebAssembly (wasm) compiled [Open Policy Agent](https://www.openpolicyagent.org/) policies
with dotnet core.

Initial implementation was based
on [Open Policy Agent WebAssemby NPM Module](https://github.com/open-policy-agent/npm-opa-wasm)

For more information check out [the guide](https://me-viper.github.io/OpaDotNet/).

## Key Features

* Fast in-process OPA policies evaluation.
* Full ABI [support](https://me-viper.github.io/OpaDotNet/articles/ABI.html).
* Additional OPA [built-ins](https://me-viper.github.io/OpaDotNet/articles/Builtins.html).
* [Compilation](https://github.com/me-viper/OpaDotNet.Compilation).
* AspDotNet Core [integration](https://github.com/me-viper/OpaDotNet.Extensions).

## NuGet Packages

|                 | Official | Preview |
|-----------------|----------|---------|
| OpaDotNet.Wasm  | [![NuGet](https://img.shields.io/nuget/v/OpaDotNet.Wasm.svg)](https://www.nuget.org/packages/OpaDotNet.Wasm/) | [![Nuget](https://img.shields.io/nuget/vpre/OpaDotNet.Wasm.svg)](https://www.nuget.org/packages/OpaDotNet.Wasm/)  |
| [OpaDotNet.Extensions.AspNetCore](https://github.com/me-viper/OpaDotNet.Extensions) | [![NuGet](https://img.shields.io/nuget/v/OpaDotNet.Extensions.AspNetCore.svg)](https://www.nuget.org/packages/OpaDotNet.Extensions.AspNetCore/) | [![Nuget](https://img.shields.io/nuget/vpre/OpaDotNet.Extensions.AspNetCore.svg)](https://www.nuget.org/packages/OpaDotNet.Extensions.AspNetCore/)  |
| [OpaDotNet.Compilation.Cli](https://github.com/me-viper/OpaDotNet.Compilation)             | [![NuGet](https://img.shields.io/nuget/v/OpaDotNet.Compilation.Cli.svg)](https://www.nuget.org/packages/OpaDotNet.Compilation.Cli/) | - |
| [OpaDotNet.Compilation.Interop](https://github.com/me-viper/OpaDotNet.Compilation)         | [![NuGet](https://img.shields.io/nuget/v/OpaDotNet.Compilation.Interop.svg)](https://www.nuget.org/packages/OpaDotNet.Compilation.Interop/) | - |

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
The result will be an OPA bundle with the `policy.wasm` binary included. See [./samples](./samples) for a more
comprehensive example.

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

## 3rd Party Libraries and Contributions

* [OPA](https://www.openpolicyagent.org/) - An open source, general-purpose policy engine that unifies policy
  enforcement across the stack.
* [Moq](https://github.com/moq/moq4) - The most popular and friendly mocking library for .NET.
* [xUnit.net](https://xunit.net/) - Free, open source, community-focused unit testing tool for the .NET Framework.
* [wasmtime-dotnet](https://github.com/bytecodealliance/wasmtime-dotnet) - .NET embedding of Wasmtime.
* [IPNetwork2](https://github.com/lduchosal/ipnetwork) - Utility classes take care of complex network, IPv4, IPv6, CIDR
  calculation for .NET developers.
* [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) - Powerful .NET library for benchmarking.
* [Semver](https://github.com/maxhauser/semver) - Implementation in .Net based on the v2.0.0 of the spec.
* [json-everything](https://github.com/gregsdennis/json-everything) - Set of libraries that ensure that common JSON functionality has good support in the System.Text.Json space.
* [YamlDotNet](https://github.com/aaubry/YamlDotNet) - YamlDotNet is a YAML library for netstandard and other .NET runtimes.
