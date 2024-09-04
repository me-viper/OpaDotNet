# Changelog

## OpaDotNet.Compilation.Abstraction v2.0.0 (2024-08-06)

### Features

* Support `--v1-compatible`, `--follow-symlinks` and `--revision` compiler flags
* Public API improvements
* Add API to write bundle manifest to `BundleWriter`

### BREAKING CHANGES

* `RegoCompilerOptions` class have been removed. All compilation options are configured using `CompilationParameters`

## OpaDotNet.Compilation.Abstraction v1.6.0 (2024-01-10)

### Features

* Move OpaDotNet.Compilation.Abstraction into separate repository for more consistent versioning.
