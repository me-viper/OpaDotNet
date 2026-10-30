---
paths:
  - "docs/**"
---

# Docs (DocFX site)

## Layout

- `docfx.json` — DocFX config. API metadata is extracted from the `OpaDotNet*.dll`s in the **snippets build output**
  (`snippets/artifacts/bin/**/debug_net8.0/`), not from `src/` directly, so `docs/snippets` must be built
  (`dotnet build -f net8.0 ./docs/snippets/Snippets.csproj`) before running `docfx docs/docfx.json`.
- `toc.yml` — top nav (`Docs` → `articles/`, `Api` → `api/`).
- `articles/` — hand-written guide. `articles/toc.yml` is the sidebar: entries without `href` are section headers
  (OPA Compatibility, Evaluation, Compilation, Reference). Adding a new article means adding it to `articles/toc.yml`,
  or it won't be reachable from navigation.
- `api/` — generated API reference (`.yml` files are gitignored via `api/.gitignore`). Never hand-edit them; fix the
  XML doc comments in `src/` instead. Only `api/index.md` is tracked.
- `snippets/` — compiled xUnit project (`Snippets.csproj`, part of `OpaDotNet.slnx`) holding all C# sample code used by
  the guide. Its `Directory.Build.targets` copies referenced `OpaDotNet*.xml` doc files next to the output so DocFX
  can pick them up.
- `images/` — image resources. `template/public/main.js` — modern-template overrides. `_site/` — build output
  (gitignored). `index.md` just redirects to `articles/WhatIs.html`.

## Code samples

- Don't inline C# samples in articles. Put the code in a `[Fact]` in `snippets/` (class `DocSamples`, partial, marked
  `[Trait("Snippet", "true")]`), wrap the shown part in `#region <Name>` / `#endregion`, and reference it with
  `[!code-csharp[](~/snippets/<File>.cs#<Name>)]`. This keeps samples compiling and running as tests.
- Usings shown in docs live in their own `#region` at the top of the snippet file (duplicate usings are allowed there
  under `#pragma warning disable CS0105`).
- Non-C# sample files (`.rego`, `.json`) go under `snippets/<topic>/` and are referenced with
  `[!code-rego[](~/snippets/<topic>/<file>.rego)]`. Any data file a snippet test reads at runtime must be added to
  `Snippets.csproj` with `CopyToOutputDirectory`.
- Short shell commands (`dotnet add package ...`, `opa build ...`) and `diff` blocks may stay inline.
- Snippet code follows the repo C# style (file-scoped namespaces, `Async` suffix, `CancellationToken`, disposing
  evaluators/streams, `p` for single-arg lambdas).

## Content conventions

- `articles/Builtins.md` tracks built-in implementation status per OPA version: when implementing a new built-in,
  add/update its row (link to the OPA policy reference, `:white_check_mark:`, and the `OpaDotNet.Wasm` version that
  first ships it — check `./build/versions.ps1`), and bump the "as for version vX.Y.Z" line if the OPA version changed.
  The `update-builtins` skill checks the table against the code and the pinned OPA version.
- `articles/ABI.md` lists supported OPA wasm ABI versions; update it when ABI support changes.
- Breaking changes to public API need an entry in `articles/migration.md`.
- Link to upstream OPA docs (`https://www.openpolicyagent.org/docs/...`) rather than re-explaining Rego semantics.

## Building / publishing

- Local preview: build snippets (above), then `docfx docs/docfx.json --serve`. Requires the Interop native lib to be
  present since `Snippets.csproj` references `OpaDotNet.Compilation.Interop`.
- Publishing is manual: `gh workflow run docs.yml --ref release/v{version} -f release=true` (run on the release
  branch so nbgv computes a stable version for the site title); it deploys `docs/_site` to `gh-pages`.
