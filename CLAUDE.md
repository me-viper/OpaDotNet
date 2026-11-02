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

Other top-level folders:

- **`build/`** — PowerShell helper scripts:
  - `versions.ps1` — prints what nbgv computes for every `version.json` (see Versioning).
  - `api-ship.ps1` — moves `PublicAPI.Unshipped.txt` entries into `PublicAPI.Shipped.txt` (release prep only).
  - `update-opa.ps1` — bumps the OPA Go dependency in `src/Compilation/Interop/opa-native/go.mod` (latest or
    `-Version`); the full OPA upgrade procedure is the `update-opa` skill.
  - `sync-opa-tests.ps1` — re-syncs `src/Wasm/tests/SdkV1/v1` from OPA's upstream test suite; defaults to the OPA
    version pinned in `src/Compilation/Interop/opa-native/go.mod` (override with `-Ref`).
  - `publish.ps1` — local `dotnet pack` + NuGet push (CI's `Publish` workflow is the normal path).
  - `qodana.ps1` — runs a local Qodana scan in Docker.
- **`docs/`** — DocFX site (`docfx.json`, `toc.yml`): guide content in `articles/`, generated API reference in `api/`,
  compiled code samples in `snippets/`, site theme overrides in `template/`. `_site/` is build output (gitignored).
  See Docs below.
- **`.github/workflows/`** — `ci.yml` (build/test/pack, reused by `publish.yml`), `publish.yml` (manual NuGet
  publish), `docs.yml` (DocFX → `gh-pages`), `opa-native.yaml` (reusable job building the Interop native lib),
  `code-quality.yaml` (Qodana).
- **`.config/dotnet-tools.json`** — local dotnet tools (`nbgv`, `dotnet-coverage`); run `dotnet tool restore` first.
- **`artifacts/`**, **`TestResults/`** — build/pack and test output (gitignored).

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

- **Root `version.json`** (e.g. `2026.2.2-preview`, year.major.build) is the *release-train* version: it drives the git
  tag (`v{version}`) and release branch name (`release/v{version}`) when cutting a release, and its value (without
  `-preview`) is what `CHANGELOG.md` entries are headed with (`## OpaDotNet {version}`). On `main` it holds the
  *next* release-train version.
- **Per-package `version.json`** (`src/Wasm/version.json`, `src/Compilation/version.json`,
  `src/Compilation.Abstractions/version.json`, `src/Extensions.AspNetCore/version.json`) each declare their own
  independent `major.minor` (e.g. Wasm is `3.2-preview`) — this is the actual NuGet package version.
  `pathFilters` scopes git-height calculation (the patch number) to commits touching that package's `src/`, so the
  patch component auto-increments only when that package actually changes. A package's filters also list the `src/` of
  its in-repo dependencies, so its patch moves whenever those change too:
  - `src/Wasm/version.json` covers `Wasm/src` and `Wasm/Directory.Build.props` (global usings for `Wasm/src`).
  - `src/Compilation/version.json` covers `Compilation/Cli/src`, `Compilation/Interop/src`,
    `Compilation/Interop/opa-native` (Go source of the native lib) and `Compilation.Abstractions/src`, so
    `OpaDotNet.Compilation.Cli` and `OpaDotNet.Compilation.Interop` share one version number.
  - `src/Compilation.Abstractions/version.json` covers only its own `src/`; it is versioned separately even though
    the Compilation packages depend on it.
  - `src/Extensions.AspNetCore/version.json` covers its own `src/` plus `Wasm/src`, `Wasm/Directory.Build.props`
    and `Compilation.Abstractions/src`.

  When adding a project reference or a file that affects a package's build output (e.g. a nested
  `Directory.Build.props`), add it to the `pathFilters` of that package and of every package depending on it.
- The patch number is the git height since `major.minor` last changed, so it is **not** reset at release time: if the
  minor bump was committed earlier, the release ships as e.g. `3.2.18`, not `3.2.0`. Changing the root `version.json`
  (or any file outside a package's `pathFilters`) doesn't reset package heights.
- **Stable vs. preview:** every `version.json` on `main` carries a `-preview` prerelease tag, so `main` builds produce
  preview packages (e.g. `3.2.25-preview`). Only the `release/v*` branch strips it, giving stable packages (`3.2.25`).
  Changing only the prerelease tag doesn't reset git height, so a preview and its release share the same patch number.
  `publicReleaseRefSpec` (`main`, `release/v*`) only controls the `-g{commit}` suffix: builds of other branches get
  it, while builds of these two branches don't.

Practical rules:

- Only bump a package's `major.minor` in its own `version.json` for a breaking change or notable feature, and keep the
  `-preview` tag on `main` (e.g. `3.2-preview` → `3.3-preview`). The patch number takes care of itself via git height.
  Don't bump a package's version for changes that don't touch its `src/`.
- Inspect what nbgv currently computes for every project with `./build/versions.ps1` (wraps
  `dotnet nbgv get-version --project <dir>` for each `version.json` in the repo) before deciding what to bump.
- Publishing itself is manual: the `Publish` workflow (`workflow_dispatch`) builds via `ci.yml` with `release: true`
  and pushes the resulting `.nupkg`/`.snupkg` to NuGet — pushing a version bump alone doesn't publish anything.

## Docs

`docs/` is a DocFX site; see `.claude/rules/docs.md` for layout, snippet conventions and publishing.

## Style notes

- File-scoped namespaces, `max_line_length = 130` for C# (set in `.editorconfig`'s `[*.{cs,vb}]` section; long
  string literals such as test data may exceed it).
- Markdown: wrap prose at about 120 characters. It's a soft guide, not enforced; tables and URLs can run longer.
- `CA2000` (dispose objects before losing scope) is enforced as a warning — evaluators, streams, etc. must be
  disposed correctly.
- Async methods must have an `Async` suffix (enforced as a warning via a `dotnet_naming_rule`).
- If lambda delegate accepts single argument call it `p`.
- Code that exists only on some target frameworks (e.g. `#if NET9_0_OR_GREATER` around `Base64Url`) needs its
  `using` wrapped in the same `#if`. `jb cleanupcode` analyzes one target framework and deletes usings that look
  unused there, which breaks the build for the others. After running it, build all target frameworks
  (`dotnet build -c Release`), not just one.

## Releasing

Releases follow the `release` skill (`.claude/skills/release/SKILL.md`), run with `/release [version]`.
