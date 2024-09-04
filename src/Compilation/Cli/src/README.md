# About

`Rego` compiler which uses `opa` cli too for policy bundle compilation and packaging.

Basically, it's equivalent to executing [`opa build`](https://www.openpolicyagent.org/docs/latest/cli/#opa-build).

## Example

**Important**. You will need `opa` cli tool v0.20.0+ to be in your PATH or provide full path
in `RegoCliCompilerOptions`.

```csharp
using OpaDotNet.Compilation.Cli;

var compiler = new RegoCliCompiler();

// Equivalent to: opa built -t wasm -e 'example/hello' ./example.rego
var bundleStream = await compiler.CompileFileAsync("example.rego", new() { Entrypoints = new HashSet<string>(["example/hello"]) });
...
```
