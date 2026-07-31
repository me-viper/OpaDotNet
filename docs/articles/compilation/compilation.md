# Policy Compilation

To evaluate policy modules they need to be compiled into WASM.

You can compile them manually with `opa build -t wasm ...` [command](https://www.openpolicyagent.org/docs/latest/cli/#opa-build) or ask OpaDotNet do that for you.

OpaDotNet provides two options for policy compilation:

- [`OpaDotNet.Compilation.Cli`](https://github.com/me-viper/OpaDotNet.Compilation/tree/main/src/OpaDotNet.Compilation.Cli) - wrapper over `opa` CLI [tool](https://www.openpolicyagent.org/docs/latest/cli).
- [`OpaDotNet.Compilation.Interop`](https://github.com/me-viper/OpaDotNet.Compilation/tree/main/src/OpaDotNet.Compilation.Interop) - wrapper over OPA SDK.

Which one you should be using?

Use `OpaDotNet.Compilation.Cli` if you have `opa` CLI tool available or you need functionality besides compilation (running tests, syntax checking etc.). Suitable for web applications and/or applications running in Docker containers. See [README](https://github.com/me-viper/OpaDotNet.Compilation/blob/main/src/OpaDotNet.Compilation.Cli/README.md) for more details.

Use `OpaDotNet.Compilation.Interop` if you need compilation only and want to avoid having external dependencies. Suitable for libraries, console application etc. See [README](https://github.com/me-viper/OpaDotNet.Compilation/blob/main/src/OpaDotNet.Compilation.Interop/README.md) for more details.

If you sign your bundles to protect against tampering, see [Bundle Signature Validation](signing.md) for how to
verify signatures when loading a bundle.

## Building bundles with `BundleWriter`

Both compilers accept a bundle directory path *or* a bundle as a `Stream` (`CompileBundleAsync`). If your policy
sources and data don't live on disk as a directory - e.g. they come from a database, embedded resources, or are
generated at runtime - use [BundleWriter](xref:OpaDotNet.Compilation.Abstractions.BundleWriter) to assemble a bundle
`Stream` in memory instead of writing files to a temp folder first.

[!code-csharp[](~/snippets/Snippets.cs#BuildBundleWithBundleWriter)]

Other things [BundleWriter](xref:OpaDotNet.Compilation.Abstractions.BundleWriter) can do:

- `WriteFile` - add the contents of an existing file on disk as a bundle entry.
- `WriteManifest` / the `manifest` constructor parameter - add bundle manifest (`.manifest`: roots, revision, metadata) into the
  bundle.
- `WriteBundle` - merge the entries of another bundle with *sources* into this one, useful for combining several policies into a single bundle.
- `FromDirectory` - create a `BundleWriter` pre-populated from a source directory, optionally excluding files
  matching glob patterns.
- `MergeCapabilities` - a static helper that merges two `capabilities.json` files (e.g. built-in capabilities plus
  your own [custom builtins](../Builtins.md)) into one.

> [!NOTE]
> `BundleWriter` strips a UTF-8 byte-order-mark from entries content if present - `opa build` fails to parse `.rego`
> files that have one.

`BundleWriter` writes a bundle of *sources* (`.rego`/`.json`/`.yaml`), not compiled wasm - you still need to run it
through [`RegoCliCompiler`](cli.md) or [`RegoInteropCompiler`](interop.md) to get an evaluable `policy.wasm` bundle.
