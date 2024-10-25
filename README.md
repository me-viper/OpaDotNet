[![CI](https://github.com/me-viper/OpaDotNet/workflows/CI/badge.svg)](https://github.com/me-viper/OpaDotNet)
[![Coverage Status](https://coveralls.io/repos/github/me-viper/OpaDotNet/badge.svg)](https://coveralls.io/github/me-viper/OpaDotNet)

# Open Policy Agent (OPA) dotnet core SDK

This is SDK for using WebAssembly (wasm) compiled [Open Policy Agent](https://www.openpolicyagent.org/) policies
with dotnet core.

For more information check out [the guide](https://me-viper.github.io/OpaDotNet/).

## Key Features

* Fast in-process OPA policies [evaluation](./src/Wasm/).
* Full ABI [support](https://me-viper.github.io/OpaDotNet/articles/ABI.html).
* Additional OPA [built-ins](https://me-viper.github.io/OpaDotNet/articles/Builtins.html).
* [Compilation](./src/Compilation/).
* AspDotNet Core [integration](./src/Extensions.AspNetCore/).

## NuGet Packages

|                 | Official | Preview |
|-----------------|----------|---------|
| OpaDotNet.Wasm  | [![NuGet](https://img.shields.io/nuget/v/OpaDotNet.Wasm.svg)](https://www.nuget.org/packages/OpaDotNet.Wasm/) | [![Nuget](https://img.shields.io/nuget/vpre/OpaDotNet.Wasm.svg)](https://www.nuget.org/packages/OpaDotNet.Wasm/)  |
| OpaDotNet.Extensions.AspNetCore | [![NuGet](https://img.shields.io/nuget/v/OpaDotNet.Extensions.AspNetCore.svg)](https://www.nuget.org/packages/OpaDotNet.Extensions.AspNetCore/) | [![Nuget](https://img.shields.io/nuget/vpre/OpaDotNet.Extensions.AspNetCore.svg)](https://www.nuget.org/packages/OpaDotNet.Extensions.AspNetCore/)  |
| OpaDotNet.Compilation.Cli             | [![NuGet](https://img.shields.io/nuget/v/OpaDotNet.Compilation.Cli.svg)](https://www.nuget.org/packages/OpaDotNet.Compilation.Cli/) | [![NuGet](https://img.shields.io/nuget/vpre/OpaDotNet.Compilation.Cli)](https://www.nuget.org/packages/OpaDotNet.Compilation.Cli/) |
| OpaDotNet.Compilation.Interop         | [![NuGet](https://img.shields.io/nuget/v/OpaDotNet.Compilation.Interop.svg)](https://www.nuget.org/packages/OpaDotNet.Compilation.Interop/) | [![Nuget](https://img.shields.io/nuget/vpre/OpaDotNet.Compilation.Interop)](https://www.nuget.org/packages/OpaDotNet.Compilation.Interop/) |

## Getting started

* [Basics](./src/Wasm/src/README.md)
* [ApsNetCore](./src/Extensions.AspNetCore/src/README.md)
* [Compilation](./src/Compilation/README.md)

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
* [OPA SDK](https://pkg.go.dev/github.com/open-policy-agent/opa/sdk) - High-level API for embedding OPA inside of Go programs.
* [.NEXT](https://github.com/dotnet/dotNext) - Powerful libraries aimed to improve development productivity and extend .NET API with unique features.
