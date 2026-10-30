---
name: update-builtins
description: >-
  Keep docs/articles/Builtins.md (OPA built-in implementation status) in sync with the code and the pinned OPA version.
  Use after bumping OPA in src/Compilation/Interop/opa-native/go.mod or syncing OPA tests, after implementing or fixing
  a built-in in OpaDotNet.Wasm, when preparing a release, or when asked to check, refresh or update the built-ins list
  or its links.
---

# Update the built-ins list

`docs/articles/Builtins.md` lists every OPA built-in that OpaDotNet.Wasm has to implement in .NET (OPA compiles the
rest into WASM itself), grouped by category, with status and the `OpaDotNet.Wasm` version that first shipped it. Its
header says which OPA version the list is for (`as for version vX.Y.Z`); that should match the OPA version pinned in
`src/Compilation/Interop/opa-native/go.mod`.

## 1. Run the check

```pwsh
./.claude/skills/update-builtins/check-builtins.ps1 -CheckLinks   # -OpaVersion vX.Y.Z to compare against another version
```

It's read-only and reports:

- **Added / removed in OPA since the header version** — each addition is tagged `native wasm, no row needed`,
  `already in doc`, or untagged (needs a row).
- **Need .NET implementation but missing from doc** — host built-ins OPA doesn't compile into WASM and that have no row.
  Deprecated built-ins, infix operators (`assign`, `eq`), `print` and `internal.*` are excluded already.
- **Implemented but missing from doc / marked done but not implemented / implemented but marked missing** — drift
  between the table and the `"name" => ...` switch arms in `src/Wasm/src/DefaultOpaImportsAbi*.cs`.
- **In doc but not an OPA built-in** — rows for built-ins OPA removed or renamed.
- **Rows with broken or missing links** (`-CheckLinks`) — checked against anchors on the live OPA docs pages.

All sections empty and the header matching go.mod means the list is current — stop there.

## 2. Fix what it reports

- **Header** — set `as for version vX.Y.Z` to the go.mod OPA version once the rows are reconciled.
- **New row** — add it to the matching category table (create a `## Category` section if none fits), keeping the
  table's order. Format:

  ```markdown
  | [strings.split_n](https://www.openpolicyagent.org/docs/policy-reference/builtins/strings#builtin-strings-stringssplit_n) | :white_check_mark: | v3.2.0 |
  ```

  Link pattern: `https://www.openpolicyagent.org/docs/policy-reference/builtins/{page}#builtin-{page}-{name without dots}`,
  where `{page}` is the category page on OPA's [built-ins index](https://www.openpolicyagent.org/docs/policy-reference/builtins)
  (e.g. `tokens`, `tokensign`, `encoding`, `providers.aws`). Unimplemented built-ins get `| - | - |`.
- **Version column** — the `OpaDotNet.Wasm` version that first ships the built-in: the `major.minor` from
  `src/Wasm/version.json` with patch `0` for unreleased work (e.g. `v3.2.0`), matching the existing rows. Don't
  change the version of rows that are already shipped.
- **Known differences** — if behavior differs from OPA's (see `src/Wasm/gen/ignore.yaml` for the cases we skip), mark
  the version `(*)` and add or reuse a `\* ...` footnote under the table.
- **Implemented but not in doc / marked missing** — add the row or flip `-` to `:white_check_mark:` with a version.
- **Marked done but not implemented** — investigate before editing: the name in the switch arm may be misspelled, or the
  implementation may live somewhere the script doesn't scan.
- **Broken links** — fix the page/anchor. If OPA's index links a page that returns 404 (it happens right after a
  release, e.g. `uri` in v1.19.0), keep the link following the pattern and mention it to the user.

## 3. Finish

```pwsh
dotnet run "$HOME/.claude/skills/prettify-markdown-tables/PrettifyMarkdownTables.cs" -- docs/articles/Builtins.md
dotnet build -f net8.0 ./docs/snippets/Snippets.csproj; docfx docs/docfx.json   # expect 0 warnings
./.claude/skills/update-builtins/check-builtins.ps1 -CheckLinks                    # confirm sections are empty
```

If a new built-in was implemented, also make sure `CHANGELOG.md` mentions it (`* Implement \`name\``) under the
upcoming `OpaDotNet.Wasm` section. Commit as `docs: ...`.
