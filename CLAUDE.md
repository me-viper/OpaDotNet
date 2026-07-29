# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

.NET SDK for evaluating WebAssembly-compiled [Open Policy Agent](https://www.openpolicyagent.org/) (OPA/Rego) policies
in-process, plus tooling to compile `.rego` sources into `.wasm` bundles and an ASP.NET Core authorization integration.
Multi-targets `net10.0;net9.0;net8.0`.

## Solution layout

Open `OpaDotNet.slnx` (not a `.sln`). Projects under `src/`:

- **`Wasm`** — the core evaluator (`OpaDotNet.Wasm`). Loads a compiled OPA wasm module via `wasmtime-dotnet`, wires up
  the OPA ABI (imports/builtins), and exposes `IOpaEvaluator` / `IOpaEvaluatorFactory` for evaluating policies.
  - `Internal/` — `WasmPolicyEngine`, `OpaWasmEvaluator`: the actual wasmtime interop and ABI plumbing. Not public API.
  - `DefaultOpaImportsAbi.*.cs` — built-in implementations of OPA's standard library (strings, time, crypto, net,
    json, jwt), split by category as partial classes.
  - `Builtins/` — extensibility point for **custom** built-ins: implement `IOpaCustomBuiltins` and mark methods with
    `[OpaCustomBuiltin("name")]`; `CompositeImportsHandler`/`ImportsCache` dispatch calls from wasm into them.
  - `GoCompat/` — shims to match Go's runtime semantics (time, big int JSON, X.509) since OPA itself is written in Go
    and policies can depend on Go-specific formatting/behavior.
  - `Rego/` — `RegoSet`, `OpaJsonReader`: handling OPA's JSON dialect (e.g. sets are represented as objects on the wire).
  - `gen/` (`OpaDotNet.Wasm.Generators`) — an incremental source generator that parses OPA's own upstream YAML test
    suite (`Wasm/tests/SdkV1/v1/**/*.yaml`) and emits xUnit test cases (`SdkV1Tests.cs` consumes the generated code).
    This is how the project verifies ABI/builtin compatibility against OPA's reference test cases. `gen/ignore.yaml`
    excludes known-unsupported cases.
- **`Compilation.Abstractions`** — `IRegoCompiler` interface and bundle types shared by the two compiler backends.
- **`Compilation/Cli`** (`OpaDotNet.Compilation.Cli`) — compiles `.rego` → `.wasm` by shelling out to the `opa` CLI
  binary. No native dependency; needs `opa` on `PATH` (or configured path).
- **`Compilation/Interop`** (`OpaDotNet.Compilation.Interop`) — compiles in-process via a native library
  (`Opa.Interop.dll`/`.so`) built from Go source in `Compilation/Interop/opa-native/` (cgo bindings around the OPA Go
  compiler). This native lib must be built separately — see Building below — it is not compiled by `dotnet build`.
- **`Extensions.AspNetCore`** — ASP.NET Core authorization integration: policy sources (file system, config, compiled
  bundle, watched), an `IAuthorizationHandler` (`OpaPolicyHandler`) that evaluates a compiled policy against the
  current `HttpContext`/claims, and a pooled evaluator factory (`OpaEvaluatorPoolProvider`) for concurrent request
  handling. Has runnable samples under `samples/` (`WebApp`, `YarpApp`, `CustomBuiltins`).
- **`Testing`** (`OpaDotNet.InternalTesting`) — shared test-only helpers, notably `TestingCompiler`, which picks
  between the CLI and Interop compiler backend at test time based on env vars (see Testing below).
- **`Common`** — tiny shared internal utility (`NopDisposable`), used across projects.

Public API surface (`Wasm`, `Compilation.Abstractions`, `Compilation/Cli`, `Compilation/Interop`,
`Extensions.AspNetCore`) is tracked with Roslyn's public API analyzer (`PublicAPI.Shipped.txt` /
`PublicAPI.Unshipped.txt` per project). Adding/removing/changing a public member requires updating the project's
`PublicAPI.Unshipped.txt`, or the build fails (`Release` builds treat warnings as errors).

## Building

```pwsh
./build.ps1                      # builds Interop native lib, then `dotnet build`
dotnet build -c Release
```

The `Compilation.Interop` native library is **not** produced by `dotnet build` — it must be built first via
`./src/Compilation/Interop/build.ps1` (what `build.ps1` at the repo root does). Building it requires Go 1.20+, and on
Windows requires WSL 2 (the Linux `.so` is cross-compiled via `gcc-mingw-w64` under WSL); see
`src/Compilation/README.md` for the full prerequisite list. If you're not touching `Compilation.Interop` or its
native bindings, you generally don't need to rebuild it — CI restores a prebuilt artifact.

## Testing

Test projects use **xUnit v3 on the Microsoft.Testing.Platform runner** (`global.json` pins
`test.runner: Microsoft.Testing.Platform`; test `.csproj`s are `OutputType=Exe`), not the classic VSTest runner.

```pwsh
dotnet test                                                     # all tests
dotnet test --filter-not-trait "Compiler=Interop"               # skip native-interop-dependent tests (what CI does on Linux)
dotnet test <path-to-test.csproj> -- --filter-method "*MethodName*"   # single test, MTP filter syntax after `--`
```

Compilation tests (`src/Compilation/tests`) parametrize over the two `IRegoCompiler` backends via
`[Trait("Compiler", "Cli"|"Interop")]`; `TestingCompiler` (in `OpaDotNet.InternalTesting`) selects the backend at
runtime from the `OPA_TEST_COMPILER` env var (`cli` or default `interop`), and `OPA_TEST_COMPILER_CLI_PATH` overrides
the `opa` binary location for the CLI backend. Running the CLI-backed tests requires the `opa` CLI on `PATH`.

`Wasm/tests/SdkV1/` contains OPA's own upstream Rego test-suite YAML files; these are compiled into test cases at
build time by the `OpaDotNet.Wasm.Generators` source generator (see Solution layout above) — don't hand-edit
generated output, edit the YAML or the generator instead.

## Versioning

Versions are computed by Nerdbank.GitVersioning (`nbgv`) from `version.json` files, and there are two independent
tiers of them — don't confuse the two:

- **Root `version.json`** (`2026.1.2`, year.major.build) is the *release-train* version: it drives the git tag
  (`v{version}`) and release branch name (`release/v{version}`) when cutting a release, and its value is what
  `CHANGELOG.md` entries are headed with (`## OpaDotNet {version}`). `publicReleaseRefSpec` means only `main` and
  `release/v*` builds get a stable version; other branches get an unstable/prerelease version.
- **Per-package `version.json`** (`src/Wasm/version.json`, `src/Compilation/version.json`,
  `src/Compilation.Abstractions/version.json`, `src/Extensions.AspNetCore/version.json`) each declare their own
  independent `major.minor` (e.g. Wasm is `3.1`) — this is the actual NuGet package version. `pathFilters` scopes
  git-height calculation (the patch number) to commits touching that package's `src/`, so the patch component
  auto-increments only when that package actually changes. `src/Compilation/version.json`'s `pathFilters` covers
  both `Compilation/Cli/src` and `Compilation/Interop/src`, so `OpaDotNet.Compilation.Cli` and
  `OpaDotNet.Compilation.Interop` share one version number; `Compilation.Abstractions` is versioned separately even
  though the other two packages depend on it.

Practical rules:

- Only bump a package's `major.minor` in its own `version.json` for a breaking change or notable feature — the patch
  number takes care of itself via git height. Don't bump a package's version for changes that don't touch its `src/`.
- Inspect what nbgv currently computes for every project with `./build/versions.ps1` (wraps
  `dotnet nbgv get-version --project <dir>` for each `version.json` in the repo) before deciding what to bump.
- When preparing a release: bump the relevant package `version.json`(s), bump the root `version.json`, and add a
  `CHANGELOG.md` entry (`## OpaDotNet {root version}` with a `### {PackageName}` subsection per changed package) —
  see recent `chore: Prepare vX.Y.Z release` commits for the pattern.
- Run `build/api-ship.ps1` as part of release prep: it moves every project's `PublicAPI.Unshipped.txt` entries into
  `PublicAPI.Shipped.txt` (sorted) and resets `Unshipped.txt`. Do this only when actually releasing, not for interim
  commits that add public API.
- Publishing itself is manual: the `Publish` workflow (`workflow_dispatch`) builds via `ci.yml` with `release: true`
  and pushes the resulting `.nupkg`/`.snupkg` to NuGet — pushing a version bump alone doesn't publish anything.

## Docs

`docs/` is a DocFX site (guide content in `docs/articles/`, API reference auto-generated into `docs/api/` — don't
hand-edit those `.yml` files). `docs/snippets/` is a compiled project so example code in the guide is guaranteed to
build; update snippets there rather than inlining unchecked code samples in docs.

## Style notes

- File-scoped namespaces, `max_line_length = 130` (see `.editorconfig` for the full rule set).
- `CA2000` (dispose objects before losing scope) is enforced as a warning — evaluators, streams, etc. must be
  disposed correctly.
- Async methods must have an `Async` suffix (enforced as a warning via a `dotnet_naming_rule`).
- If lambda delegate accepts single argument call it `p`.
