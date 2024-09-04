# Changelog

## OpaDotNet.Compilation v2.0.0 (2024-08-06)

### Bug Fixes

* Cli compiler should use relative path when compiling from stream ([91b7c2e](https://github.com/me-viper/OpaDotNet.Compilation/commit/91b7c2ebd62387eafa65509812763dc1e6892968))

### Features

* Support new compiler flags ([8e9954a](https://github.com/me-viper/OpaDotNet.Compilation/commit/8e9954a1aabdefa0199788863a8b4434ab802a75))

### BREAKING CHANGES

* `RegoCompilerOptions` class have been removed. All compilation options are configured using `CompilationParameters`

## OpaDotNet.Compilation v1.7.4 (2024-08-06)

### Bug Fixes

* Fix interop parameters marshaling

## OpaDotNet.Compilation v1.7.3 (2024-06-14)

### Bug Fixes

* Fix interop compiler to parse metadata when compiling from stream

## OpaDotNet.Compilation v1.7.2 (2024-06-13)

### Features

* Improve interop compilation from stream

## OpaDotNet.Compilation v1.7.1 (2024-06-12)

### Bug Fixes

* Prevent OpaGetVersion from leaking memory

## OpaDotNet.Compilation v1.7.0 (2024-05-15)

### Features

* Bump OPA version to v0.64.1
* Update dependencies

## OpaDotNet.Compilation v1.6.1 (2024-02-09)

### Features

* Move OpaDotNet.Compilation.Abstractions into separate repository ([821738d](https://github.com/me-viper/OpaDotNet.Compilation/commit/821738d7f1609120ce7429b48bdb90a1ec97d4cd))
* Fix [#5] Opa.Interop compiler fails on older ubuntu versions (20.04)

## OpaDotNet.Compilation v1.6.0 (2024-01-10)

### Features

* Bump OPA version to v0.60.0
* Update dependencies

## OpaDotNet.Compilation v1.5.0 (2023-11-23)

### Features

* Support net8.0
* Bump OPA version to v0.58.0

## OpaDotNet.Compilation v1.4.1 (2023-10-10)

### Features

* Add helper to build BundleWriter from directory
* Add ignore parameter for Cli and Interop compilers

## OpaDotNet.Compilation v1.4.0 (2023-10-05)

### Features

* Provide API to merge capabilities ([e847732](https://github.com/me-viper/OpaDotNet.Compilation/commit/e847732790bc16b844a7938db40fd9c79877b97b))
* Redesign compilation API ([2028f9e](https://github.com/me-viper/OpaDotNet.Compilation/commit/2028f9ee73e64e54514f30121ea6fd78d026e2c4))

## OpaDotNet.Compilation v1.3.2 (2023-10-04)

### Features

* Bump OPA version to v0.57.0 ([ab6dceb](https://github.com/me-viper/OpaDotNet.Compilation/commit/ab6dceb7a5a616af77d719fc262d23ccc65a08f3))

## OpaDotNet.Compilation v1.3.0 (2023-09-27)

### Bug Fixes

* Closes [#1](https://github.com/me-viper/OpaDotNet.Compilation/issues/1), [#2](https://github.com/me-viper/OpaDotNet.Compilation/issues/2). Use tempfs to avoid creating temporary files ([f5490c3](https://github.com/me-viper/OpaDotNet.Compilation/commit/f5490c371a80ea39deb0d2ab5f0fb7c8fde93853))
* Improve error handling ([c0f9b15](https://github.com/me-viper/OpaDotNet.Compilation/commit/c0f9b15a88e2dde80ef8d89bb407e2e1d2969cac))

### Features

* Bump OPA dependencies to v0.56.0 ([9d4759d](https://github.com/me-viper/OpaDotNet.Compilation/commit/9d4759d0b99a8a6a3ba612ae65a54c9a99e7ba87))

## OpaDotNet.Compilation v1.2.2 (2023-09-18)

### Bug Fixes

* Interop compiler is not removing temporary artifacts ([a52ac06](https://github.com/me-viper/OpaDotNet.Compilation/commit/a52ac06617e0dbd627197a38d2e158efe963caa3))

## OpaDotNet.Compilation v1.2.1 (2023-09-18)

### Bug Fixes

* Interop compiler is not removing temporary artifacts ([a52ac06](https://github.com/me-viper/OpaDotNet.Compilation/commit/a52ac06617e0dbd627197a38d2e158efe963caa3))

## OpaDotNet.Compilation v1.2.0 (2023-08-23)

### Bug Fixes

* Support building bundle from bundle in Cli compiler ([6b17a56](https://github.com/me-viper/OpaDotNet.Compilation/commit/6b17a5619320f1c78bfff49f726937a9ea91665a))
* Normalize path handling for bundles ([1987a1d](https://github.com/me-viper/OpaDotNet.Compilation/commit/1987a1d13a328e37bbe7a6ae1bfbbe9de128a43f))

### Features

* Implement API to construct bundles ([5425be1](https://github.com/me-viper/OpaDotNet.Compilation/commit/5425be1a25200f690ba1fcc27edf73c1ce8fa38d))
* Support compilation from bundle Stream ([310c92f](https://github.com/me-viper/OpaDotNet.Compilation/commit/310c92feed48e0d1704799efaa2a34d1005c1aed))
* Improve interop compiler ([e6d0a5e](https://github.com/me-viper/OpaDotNet.Compilation/commit/e6d0a5e469c4e7cbfeff2790f90375075bf7cc32))
* Support more compiler flags ([6381238](https://github.com/me-viper/OpaDotNet.Compilation/commit/6381238585147f05ef98d285f354810c2bb9ac03))

## OpaDotNet.Compilation v1.1.3 (2023-08-18)

### Bug Fixes

* Do bundle path normalization for interop compiler
* Do source file path normalization for interop compiler

## OpaDotNet.Compilation v1.1.1 (2023-08-17)

### Bug Fixes

* Fix entrypoints array construction

## OpaDotNet.Compilation v1.1.0 (2023-08-17)

### Bug Fixes

* Fix invalid interop call
* Fix nuget package health warnings

### Code Refactoring

* Improve naming. Support future extensions

### BREAKING CHANGES

* Native interface have been changed
