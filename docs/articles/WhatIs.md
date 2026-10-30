# What is OpaDotNet?

OpaDotNet is a .NET SDK for evaluating [Open Policy Agent](https://www.openpolicyagent.org/) (OPA) policies in-process.
You write policies in [Rego](https://www.openpolicyagent.org/docs/latest/policy-language/), compile them to
WebAssembly (WASM), and evaluate them directly inside your application - no OPA server, sidecar or network round-trip
required.

## Why OpaDotNet?

- **In-process evaluation.** Policies run inside your process via [Wasmtime](https://wasmtime.dev/), so a decision is
  a method call rather than an HTTP request.
- **OPA WASM ABI support.** Policies compiled for OPA WASM ABI up to 1.3 are supported. See
  [ABI](ABI.md) for details.
- **OPA built-ins in .NET.** Built-in functions that OPA doesn't compile into WASM (strings, time, crypto, JWT, network,
  JSON, YAML and more) are implemented in .NET and checked against OPA's own test suite. See
  [Built-in Functions](Builtins.md) for the full list.
- **Custom built-ins.** Extend Rego with your own functions written in C#. See
  [Custom Built-in Functions](evaluation/builtins.md).
- **Compilation from .NET.** Compile `.rego` sources and bundles to WASM from your code, at build time or at runtime.
- **ASP.NET Core integration.** Use Rego policies as ASP.NET Core authorization policies.

## Packages

| Package | Use it to |
|---------|-----------|
| [`OpaDotNet.Wasm`](https://www.nuget.org/packages/OpaDotNet.Wasm/) | Load compiled policies (`policy.wasm` or a bundle) and evaluate them. This is the core package. |
| [`OpaDotNet.Compilation.Cli`](https://www.nuget.org/packages/OpaDotNet.Compilation.Cli/) | Compile Rego to WASM using the `opa` CLI tool. |
| [`OpaDotNet.Compilation.Interop`](https://www.nuget.org/packages/OpaDotNet.Compilation.Interop/) | Compile Rego to WASM in-process using a bundled native OPA SDK library (`linux-x64` and `windows-x64` only). |
| [`OpaDotNet.Extensions.AspNetCore`](https://www.nuget.org/packages/OpaDotNet.Extensions.AspNetCore/) | Authorize ASP.NET Core requests with Rego policies. |

You only need `OpaDotNet.Wasm` if your policies are already compiled, for example with
`opa build -t wasm` in your CI pipeline. Add a compilation package if you want to compile policies from your
application. See [Policy Compilation](compilation/compilation.md) for help choosing between the CLI and Interop
compilers.

## How it works

1. **Write** a policy in Rego.
2. **Compile** it to a WASM module or OPA bundle - with `opa build -t wasm`, or with `OpaDotNet.Compilation.Cli` or
   `OpaDotNet.Compilation.Interop`.
3. **Load** the compiled policy with `OpaDotNet.Wasm` to get an `IOpaEvaluator`.
4. **Evaluate** the policy's entrypoints against your input and data.

## Next steps

- [Quick Start](HowToStart.md) - install the package and evaluate your first policy.
- [Evaluation Basics](evaluation/basics.md) - loading policies, external data, input and evaluation.
- [Policy Compilation](compilation/compilation.md) - compile Rego from .NET.
- [ASP.NET Core](AspNet/Intro.md) - use Rego policies for request authorization.
- [Migration](migration.md) - upgrading from a previous major version.
