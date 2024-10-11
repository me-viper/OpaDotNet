![Static Badge](https://img.shields.io/badge/OPA-_v0.67.0-blue)

# Open Policy Agent (OPA) Compilation Tools

Backend for packaging OPA policy and data files into bundles for [OpaDotNet](https://github.com/me-viper/OpaDotNet) project.

## NuGet Packages

|                                       | Package  |
|---------------------------------------|----------|
| OpaDotNet.Compilation.Abstractions    | [![NuGet](https://img.shields.io/nuget/v/OpaDotNet.Compilation.Abstractions.svg)](https://www.nuget.org/packages/OpaDotNet.Compilation.Abstractions/) |
| OpaDotNet.Compilation.Cli             | [![NuGet](https://img.shields.io/nuget/v/OpaDotNet.Compilation.Cli.svg)](https://www.nuget.org/packages/OpaDotNet.Compilation.Cli/) |
| OpaDotNet.Compilation.Interop         | [![NuGet](https://img.shields.io/nuget/v/OpaDotNet.Compilation.Interop.svg)](https://www.nuget.org/packages/OpaDotNet.Compilation.Interop/) |

## Getting Started

Which one you should be using?

Use `OpaDotNet.Compilation.Cli` if you have `opa` CLI [tool](https://www.openpolicyagent.org/docs/latest/cli) installed or you need functionality besides compilation (running tests, syntax checking etc.). Suitable for web applications and/or applications running in Docker containers. See [README](./src/OpaDotNet.Compilation.Cli) for more details.

Use `OpaDotNet.Compilation.Interop` if you need compilation only and want to avoid having external dependencies. Suitable for libraries, console application etc. See [README](./src/OpaDotNet.Compilation.Interop/README.md) for more details.

For more information you can check the [guide](https://me-viper.github.io/OpaDotNet/articles/compilation/compilation.html).

### Cli

#### Install OpaDotNet.Compilation.Cli nuget package

```sh
dotnet add package OpaDotNet.Compilation.Cli
```

#### Usage

> [!IMPORTANT]
> You will need `opa` cli tool v0.67.0+ to be in your PATH or provide full path in `RegoCliCompilerOptions`.

```csharp
using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Cli;

IRegoCompiler compiler = new RegoCliCompiler();
using var bundleStream = await compiler.CompileFile("example.rego", new[] { "example/hello" });

// Use compiled policy bundle.
...
```

### Interop

#### Install OpaDotNet.Compilation.Interop nuget package

```sh
dotnet add package OpaDotNet.Compilation.Interop
```

#### Usage

```csharp
using OpaDotNet.Compilation.Abstractions;
using OpaDotNet.Compilation.Interop;

IRegoCompiler compiler = new RegoInteropCompiler();
using var bundleStream = await compiler.CompileFile("example.rego", new[] { "example/hello" });

// Use compiled policy bundle.
...
```

## Building

### Prerequisites

- go lang v1.20
- dotnet SDK 7.0
- opa cli

#### Linux (WSL)

- `gcc` to compile `Opa.Interop.so`
- `gcc-mingw-w64` to compile `Opa.Interop.dll`

#### Windows

> [!NOTE]
> WSL 2.0 is required to compile `Opa.Interop.so` on windows.

- Powershell Core 7.0+
- WSL 2.0

### Build and Test

- Run `./Interop/build.ps1` Compile [Opa.Interop](./Interop/opa-native) libraries
- Run `dotnet build` to build the project or use Visual Studio to build `OpaDotNet.sln`
- Run `dotnet test` to test the project or use Visual Studio test explorer.
