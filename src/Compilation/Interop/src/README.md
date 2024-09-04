# About

`Rego` compiler which uses interop wrapper for subset
of [OPA SDK](https://pkg.go.dev/github.com/open-policy-agent/opa/sdk) for policy bundle compilation and packaging.

**Note**. Due to inclusion of native dependencies, nuget `OpaDotNet.Compilation.Interop` package is ~15MB.

## Versions

| Version | OPA SDK Version | Platforms         |
|---------|-----------------|-------------------|
| v1.0.X  | v0.55.0         | linux-x64 win-x64 |

## Example

```csharp
using OpaDotNet.Compilation.Interop;

var compiler = new RegoInteropCompiler();
var bundleStream = await compiler.CompileFileAsync("example.rego", new() { Entrypoints = new HashSet<string>(["example/hello"]) });

// Use compiled policy bundle.
...
```
