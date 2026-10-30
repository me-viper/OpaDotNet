---
name: update-opa
description: >-
  Bump the OPA version OpaDotNet is built and tested against: the Go dependency of the Interop native library, OPA's
  upstream compatibility test cases, the built-ins docs and the changelog. Use when asked to update, bump or upgrade
  OPA (or the OPA SDK), to support a new OPA release, or to check whether a newer OPA is available.
---

# Update OPA

The OPA version is pinned in one place: `require github.com/open-policy-agent/opa vX.Y.Z` in
`src/Compilation/Interop/opa-native/go.mod`. Everything else follows it: `build/sync-opa-tests.ps1` and the
`update-builtins` skill default to that version, and CI installs Go from that `go.mod` (`setup-go` `go-version-file`).

## 0. Check what's available

```pwsh
go -C src/Compilation/Interop/opa-native list -m -f '{{.Version}} {{.Time}}' github.com/open-policy-agent/opa@latest
```

Read the OPA release notes between the current and target versions
(`https://github.com/open-policy-agent/opa/releases`) for new or changed built-ins, WASM/ABI changes and Go API
changes affecting `opa-native/main.go`. Bump to a specific version (`-Version`) unless the user asked for latest.

## 1. Update the Go dependency

```pwsh
./build/update-opa.ps1 [-Version vX.Y.Z]
```

It runs `go get` + `go mod tidy` in `opa-native`, prints `OPA: old -> new`, and flags a change of the `go` directive (a
new OPA may require a newer Go; CI follows automatically, locally you need that Go version installed, and on Windows
also inside WSL for the native build).

## 2. Rebuild the native library

```pwsh
./src/Compilation/Interop/build.ps1
```

Builds `lib/windows-amd64/Opa.Interop.dll` and `lib/linux-amd64/Opa.Interop.so` (on Windows the Linux build runs in WSL;
see `src/Compilation/README.md`). If `main.go` no longer compiles, adapt it to OPA's Go API changes. If the native build
isn't possible locally, say so and rely on CI's `opa-native.yaml` job instead of skipping silently.

## 3. Sync OPA's compatibility test cases

```pwsh
./build/sync-opa-tests.ps1
```

It replaces `src/Wasm/tests/SdkV1/v1` with the upstream test cases for the new version, stages them and lists changed
files. Then run them:

```pwsh
dotnet test src/Wasm/tests/OpaDotNet.Wasm.Tests.csproj -- --filter-class "*SdkV1Tests"
```

For failures, follow `.claude/rules/SdkV1Tests.md`: fix the built-in implementation, or exclude unsupported cases in
`src/Wasm/gen/ignore.yaml`. New test directories usually mean new built-ins — see step 4.

## 4. Built-ins

Run the `update-builtins` skill. It reports built-ins added or removed since the version in
`docs/articles/Builtins.md`, which of them OPA compiles into WASM natively, and which need a .NET implementation.
Implementing a new built-in is a separate task — ask the user before starting one; otherwise add it to the docs as not
implemented (`| - | - |`). Bump the doc's `as for version` header to the new OPA version.

## 5. Verify

```pwsh
dotnet build -c Release            # full solution, no warnings - what CI runs
dotnet test                        # includes the Interop compiler tests against the rebuilt native lib
```

Compilation tests run against both compiler backends; the CLI backend uses the `opa` binary on `PATH`, which may be
a different OPA version than the one just pinned — mention it if those tests fail only for the CLI backend.

## 6. Changelog and commit

Add to the upcoming release in `CHANGELOG.md`, replacing any older `Support OPA SDK` line in that section:

```markdown
### OpaDotNet.Compilation.Interop v{version}

* Support OPA SDK vX.Y.Z
```

Also list any built-ins implemented or fixed under `### OpaDotNet.Wasm`. Commit as `feat: Support OPA vX.Y.Z` (it's
user-visible: the Interop compiler now compiles with that OPA), including `go.mod`, `go.sum`, the synced test cases,
`ignore.yaml`, built-in changes and docs. The rebuilt `lib/` binaries are gitignored.
