---
name: release
description: >-
  Cut an OpaDotNet release: bump versions, finalize the changelog, ship public APIs, create the release branch and
  tag, publish docs, the GitHub release and the NuGet packages. Use only when the user asks to release, cut or publish
  a new version.
argument-hint: "[release-train version, e.g. 2026.2.2]"
disable-model-invocation: true
---

# Release

Cut release `$ARGUMENTS`. If no version was given, use the root `version.json` without `-preview`, and make sure it
matches the newest `## OpaDotNet {version}` heading in `CHANGELOG.md`.

Read the Versioning section of `CLAUDE.md` first: it explains the root release-train version vs. per-package versions,
the git-height patch number and the `-preview` tag on `main`.

Use `gh` for GitHub steps. Find the previous release with `git describe --tags --abbrev=0 --match 'v*'` and what's
already published with `https://api.nuget.org/v3-flatcontainer/{lowercase-package-id}/index.json`.

Steps 0–5 can be undone. Before step 6 (pushing the tag), stop and show the user the release-train version and every
package version that will be published, and wait for confirmation. Everything from step 6 on is public.

## Steps

0. Before starting, make sure the latest CI run on `main` is green (`gh run list --branch main --limit 1`). CI can
   break without code changes (e.g. NuGet audit `NU1902` warnings-as-errors for a vulnerable transitive package, or a
   new `ubuntu-latest` image missing legacy tzdata), so fix that first.
1. Check if versions need a bump: run `./build/versions.ps1`, then bump `major.minor` in the `version.json` of each
   package whose `src/` changed in a breaking or notable way. Compare against the latest versions on NuGet: the minor
   bump may already have been committed. Keep the `-preview` tag on `main`. Set the root `version.json` to
   `{version}-preview`, where `{version}` is the release-train version; it becomes the tag, so it must match the
   `CHANGELOG.md` heading (the heading may have been written ahead of time with a different number than
   `version.json`).
2. Update the `CHANGELOG.md` entry: `## OpaDotNet {version}` with a `### {PackageName} v{version}` subsection per
   changed package (see recent `chore: Prepare vX.Y.Z release` commits for the pattern).
   - Use the exact `NuGetPackageVersion` from `./build/versions.ps1`, without `-preview`, in each heading (e.g.
     `v3.2.18`, not `v3.2.0` or `v3.2.18-preview`). The prep commit doesn't touch package `src/`, so these numbers
     don't change after committing; re-run `versions.ps1` after the commit to confirm.
   - Cross-check `git log v{previous}..HEAD -- src/*/src src/Compilation/*/src` for user-visible changes missing from
     the entry, including dependency updates (e.g. Wasmtime) and fixes made after the entry was first written.
3. Ship public APIs: run `build/api-ship.ps1`. It moves every project's `PublicAPI.Unshipped.txt` entries into
   `PublicAPI.Shipped.txt` (sorted) and resets `Unshipped.txt`. Only do this when actually releasing. Confirm a
   **full-solution** `dotnet build -c Release` passes with no warnings — that's what CI runs.
4. Commit as `chore: Prepare v{version} release` and push `main`.
5. Create the release branch `release/v{version}` from `main`. On it, remove the `-preview` tag from every
   `version.json` (root and packages), run `./build/versions.ps1` to confirm the stable versions match the
   `CHANGELOG.md` headings, commit as `chore: Release v{version}`, and push the branch. This commit only touches
   `version.json` files, which are outside every package's `pathFilters`, so the patch numbers don't change.
6. Create the tag `v{version}` on that release-branch commit, not on `main`, and push it. Use a lightweight tag, like
   previous tags. The push reports bypassing a tag-creation ruleset; that's expected for the repo admin.
7. Make sure the CI run for the release branch passes, including all tests. Its `Get Version` and `Pack` steps show
   the exact package versions that will be published. They must have no `-preview` suffix.
8. Publish docs if there were documentation changes since the previous release:
   `gh workflow run docs.yml --ref release/v{version} -f release=true`. Run it on the release branch, not the tag, so
   nbgv computes the stable version for the site title. It deploys to the `gh-pages` branch.
9. Create the GitHub release from the existing tag, using the `CHANGELOG.md` section (without the `##` heading) as the
   body: `gh release create v{version} --verify-tag --title v{version} --notes-file <notes> --latest`.
10. Publish by running the `Publish` workflow manually: `gh workflow run publish.yml --ref release/v{version}`. Pushing
    the tag alone doesn't publish to NuGet. It reruns the full CI first (~15 min). In the `Push Nuget Symbols` step,
    `Conflict ... already exists` for every `.snupkg` is expected — the package push already uploaded them.
11. Confirm all packages appear on NuGet at the expected versions; indexing after the push takes ~10 minutes.
12. Start the next release train on `main`: bump the root `version.json` to the next `{version}-preview` (e.g.
    `2026.2.2-preview` → `2026.2.3-preview`), commit as `chore: Start v{next} development` and push. Package
    `version.json` files stay on `-preview` and need no change.
