# Changelog

## OpaDotNet 2026.2.2

### OpaDotNet.Wasm

* Fix `uri.parse` and `uri.is_valid` to match Go `net/url`: validate userinfo, host escapes and port, and report
  `raw_path` like OPA
* Fix built-in arguments with number literals without a leading zero (e.g. `.5`) evaluating as undefined
* Fix `yaml.is_valid` and `yaml.unmarshal` with multi-document YAML

### OpaDotNet.Extensions.AspNetCore

* Update `OpaDotNet.Wasm`

### OpaDotNet.Compilation.Interop

* Support OPA SDK v1.22.0

## OpaDotNet 2026.2.1

### OpaDotNet.Wasm v3.2.18

* Support evaluation timeout `WasmPolicyEngineOptions.Timeout`
* Add async built-ins support
* Implement `uri.is_valid` and `uri.parse`
* Implement `strings.split_n`
* Fix `strings.count` when substring is empty
* Fix `graph.reachable_paths`
* Fix custom built-ins returning `Task` reporting success when cancelled
* Fix custom built-in exceptions being wrapped in `AggregateException` or losing their stack trace
* Fix memoized custom built-ins running more than once on concurrent calls or caching failures
* Fix `EST`/`EDT` time zone abbreviations on Linux distributions without legacy tzdata (e.g. Ubuntu 24.04+)
* Improve disposal of evaluators and evaluator factories
* Update Wasmtime to v48
* Update YamlDotNet to v18 and Yaml2JsonNode to v3
* Update JsonSchema.Net to v9 and JsonPatch.Net to v5
* Update IPNetwork2 to v5

### OpaDotNet.Extensions.AspNetCore v3.2.20

* Update `OpaDotNet.Wasm`

### OpaDotNet.Compilation.Abstractions v2.2.4

* Support UTF8 entries with BOM in `BundleWriter`

### OpaDotNet.Compilation.Cli v2.3.4

* Update `OpaDotNet.Compilation.Abstractions`

### OpaDotNet.Compilation.Interop v2.3.4

* Support OPA SDK v1.19.0
* Update `OpaDotNet.Compilation.Abstractions`

## OpaDotNet 2025.2.1

.NET 10 support

### OpaDotNet.Wasm

* Implement `json.marshal_with_options`

## OpaDotNet 2025.1.1

### OpaDotNet.Wasm

* Major release

### OpaDotNet.Compilation.Abstractions

* Major release

### OpaDotNet.Compilation

* Major release

### OpaDotNet.Extensions

* Major release

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

## OpaDotNet.Extensions v2.4.1 (2024-02-12)

### Features

* Bump dependencies

## OpaDotNet.Wasm v2.5.0 (2024-02-09)

### Bug Fixes

* `TryGetRegoSet<T>` fails when `T` is JsonNode ([c9f28cb](https://github.com/me-viper/OpaDotNet/commit/c9f28cb8fa8da6567a983421f8b2a9c6c17617ea))

### Features

* [#1](https://github.com/me-viper/OpaDotNet/issues/1). Implement missing built-ins:
  * time.format
  * time.parse_ns
  * graph.reachable_paths
  * uuid.parse
  * object.subset
  * crypto.parse_private_keys
  * crypto.x509.parse_and_verify_certificates
  * crypto.x509.parse_certificates
  * crypto.x509.parse_certificate_request
  * crypto.x509.parse_keypair
  * crypto.x509.parse_rsa_private_key

## OpaDotNet.Compilation v1.6.1 (2024-02-09)

### Features

* Move OpaDotNet.Compilation.Abstractions into separate repository ([821738d](https://github.com/me-viper/OpaDotNet.Compilation/commit/821738d7f1609120ce7429b48bdb90a1ec97d4cd))
* Fix [#5] Opa.Interop compiler fails on older ubuntu versions (20.04)

## OpaDotNet.Extensions v2.4.0 (2024-01-30)

### Bug Fixes

* [#6](https://github.com/me-viper/OpaDotNet.Extensions/issues/6). Fix claim serialization ([98ea380](https://github.com/me-viper/OpaDotNet.Extensions/commit/98ea380f12fd879f876b6215428253bd14ceb8f6))

### Features

* Allow creating IHttpRequestPolicyInput instances for extensibility ([39dbc25](https://github.com/me-viper/OpaDotNet.Extensions/commit/39dbc255d82f5121aab5ad224a605af3e7f24455))
* Allow to specify applicable authentication schemes ([e2ce5de](https://github.com/me-viper/OpaDotNet.Extensions/commit/e2ce5de387fc9bccc5f2b6c84e3068579fd884b5))
* Bump OpaDotNet version ([3a72ad2](https://github.com/me-viper/OpaDotNet.Extensions/commit/3a72ad260338675592f74b1d902715141866e2d0))

## OpaDotNet.Wasm v2.4.1 (2024-01-26)

### Bug Fixes

* [#26](https://github.com/me-viper/OpaDotNet/issues/26). time.now_ns() should return time in UTC ([2eb1190](https://github.com/me-viper/OpaDotNet/commit/2eb1190d3d5fede52e795e1dbcae44ca89cce831))
* [#27](https://github.com/me-viper/OpaDotNet/issues/27), [#28](https://github.com/me-viper/OpaDotNet/issues/28). Fix token validation. ([3833e2d](https://github.com/me-viper/OpaDotNet/commit/3833e2dd1096069c09d1a0d683ac725ac3150a5b))
* Fallback to default lifetime validation if time constraint is not set ([ac6210d](https://github.com/me-viper/OpaDotNet/commit/ac6210d6785f7963e13ad7ddb8ea4188d2d6968e))

## OpaDotNet.Extensions v2.3.0 (2024-01-15)

### Bug Fixes

* [#5](https://github.com/me-viper/OpaDotNet.Extensions/issues/5). Fix AuthorizationPolicyProvider composition ([3bf0ad7](https://github.com/me-viper/OpaDotNet.Extensions/commit/3bf0ad762c2f9a512a7b761cc2f01932ed9c7ab6))

## OpaDotNet.Wasm v2.4.0 (2024-01-10)

### Bug Fixes

* [#24](https://github.com/me-viper/OpaDotNet/issues/24). Fix strings.sprintf function to use JsonSerializerOptions ([e69d916](https://github.com/me-viper/OpaDotNet/commit/e69d916f25d6070a83732e46924eb77f432ea1f5))

### Features

* Update dependencies ([9158856](https://github.com/me-viper/OpaDotNet/commit/9158856d2de0415d68ecb37953307fa1d87d94c2))

## OpaDotNet.Compilation v1.6.0 (2024-01-10)

### Features

* Bump OPA version to v0.60.0
* Update dependencies

## OpaDotNet.Extensions v2.2.0 (2024-01-10)

### Features

* Update dependencies ([0ac41b6](https://github.com/me-viper/OpaDotNet.Extensions/commit/0ac41b6bcedba9b98901bb735ea2a49cfcd6d568))

## OpaDotNet.Compilation v1.5.0 (2023-11-23)

### Features

* Support net8.0
* Bump OPA version to v0.58.0

## OpaDotNet.Wasm v2.3.0 (2023-11-21)

### Features

* [#23]. Support net8.0 ([9b7f79b](https://github.com/me-viper/OpaDotNet/commit/9b7f79bcfc51113691f9f753ad477cd58c692a89))

## OpaDotNet.Extensions v2.1.0 (2023-11-21)

### Features

* [#3](https://github.com/me-viper/OpaDotNet.Extensions/issues/3). Support net8.0 ([c702cba](https://github.com/me-viper/OpaDotNet.Extensions/commit/c702cba2dc32206612ecd4b5d683f42d03bb078c))
* [#4](https://github.com/me-viper/OpaDotNet.Extensions/issues/4). Support precompiled bundles ([d5e5ace](https://github.com/me-viper/OpaDotNet.Extensions/commit/d5e5ace324234d0cbb05b99830b5e76b3092033a))

## OpaDotNet.Compilation v1.4.1 (2023-10-10)

### Features

* Add helper to build BundleWriter from directory
* Add ignore parameter for Cli and Interop compilers

## OpaDotNet.Wasm v2.2.0 (2023-10-09)

### Bug Fixes

* print() does not respect json serialization options ([a47356f](https://github.com/me-viper/OpaDotNet/commit/a47356f76df68a6f36cdd8a54ff3b74def809bf8))

### Features

* [#1](https://github.com/me-viper/OpaDotNet/issues/1). Implement missing builtins ([8412dba](https://github.com/me-viper/OpaDotNet/commit/8412dba2f572a469e55b208519b7eecfa658b6ad))
  * glob.quote_meta
* [#21](https://github.com/me-viper/OpaDotNet/issues/21). Implement new buildins ([8ee6944](https://github.com/me-viper/OpaDotNet/commit/8ee6944dca0bb139f2db1a60fc2a7f95e2a7b286))
  * numbers.range_step
* Close [#22](https://github.com/me-viper/OpaDotNet/issues/22). Use JavaScriptEncoder.UnsafeRelaxedJsonEscaping by default ([aa9b962](https://github.com/me-viper/OpaDotNet/commit/aa9b962e04d67555763962fa85b4609f8852fe1f))

## OpaDotNet.Compilation v1.4.0 (2023-10-05)

### Features

* Provide API to merge capabilities ([e847732](https://github.com/me-viper/OpaDotNet.Compilation/commit/e847732790bc16b844a7938db40fd9c79877b97b))
* Redesign compilation API ([2028f9e](https://github.com/me-viper/OpaDotNet.Compilation/commit/2028f9ee73e64e54514f30121ea6fd78d026e2c4))

## OpaDotNet.Compilation v1.3.2 (2023-10-04)

### Features

* Bump OPA version to v0.57.0 ([ab6dceb](https://github.com/me-viper/OpaDotNet.Compilation/commit/ab6dceb7a5a616af77d719fc262d23ccc65a08f3))

## OpaDotNet.Wasm v2.1.1 (2023-09-29)

### Features

* [#11](https://github.com/me-viper/OpaDotNet/issues/11). Implement print() ([274fcfb](https://github.com/me-viper/OpaDotNet/commit/274fcfb0ed3c0c481ab37e547045b5b44e927bf1))

## OpaDotNet.Wasm v2.1.0 (2023-09-28)

### Features

* Update dependencies ([c4d591b](https://github.com/me-viper/OpaDotNet/commit/c4d591bab62f83c3efa8be1833e6d9afbda2e130))

## OpaDotNet.Extensions v2.0.0 (2023-09-28)

### Bug Fixes

* Fail if configuration has no policies ([948464c](https://github.com/me-viper/OpaDotNet.Extensions/commit/948464c540c3618f38114f27e94f38c40de2a4f9))
* Fix building bundles from configuration ([ada70b6](https://github.com/me-viper/OpaDotNet.Extensions/commit/ada70b6e0ddc23335409d892acd4228949f5c5e7))
* Fix file system monitoring in kubernetes ([4033f54](https://github.com/me-viper/OpaDotNet.Extensions/commit/4033f545815d3e1429b8275d67afdf65d1f06ec4))

### Features

* Add more ServiceCollection helpers ([5c7488f](https://github.com/me-viper/OpaDotNet.Extensions/commit/5c7488f7ffe83581b53cd3ba3f946986ea539416))
* Improve changes detection to avoid unneeded recompilation ([b767367](https://github.com/me-viper/OpaDotNet.Extensions/commit/b7673672e522a3f43023c1ccf1f54910f051299e))
* Improve PolicyHandler extensibility ([8c88c27](https://github.com/me-viper/OpaDotNet.Extensions/commit/8c88c272332729abe5afe19312ca150a3d8eb1b5))
* Update dependencies ([c4ed3d4](https://github.com/me-viper/OpaDotNet.Extensions/commit/c4ed3d42c42ea3f4298ecb79548cc03936d2d4fb))
* Support custom ABI imports ([3f16d5a](https://github.com/me-viper/OpaDotNet.Extensions/commit/3f16d5abd851a53f47bd847153b7d25ebb6817e6))

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

## OpaDotNet.Wasm v2.0.0 (2023-08-18)

### Features

* Migrate to the new compilation infrastructure ([1e3815a](https://github.com/me-viper/OpaDotNet/commit/1e3815aa10d592a0d17c7cb74fd0375993e20973))

### BREAKING CHANGES

* Compilation logic have been moved from OpaDotNet.Wasm to OpaDotNet.Compilation.Cli assembly

## OpaDotNet.Compilation v1.1.3 (2023-08-18)

### Bug Fixes

* Do bundle path normalization for interop compiler
* Do source file path normalization for interop compiler

## OpaDotNet.Extensions v1.1.0 (2023-08-18)

### Features

* Switch to new compilation backend ([b87e99b](https://github.com/me-viper/OpaDotNet.Extensions/commit/b87e99bd025cf271a03519112ed636b8a895f7e6))

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

## OpaDotNet.Extensions v1.0.5 (2023-08-17)

### Features

* Fix nuget package health warnings

## OpaDotNet.Extensions v1.0.4 (2023-08-16)

### Features

* Update OpaDotNet.Wasm to v1.4.0

## OpaDotNet.Wasm v1.4.0 (2023-08-15)

### Bug Fixes

* `CapabilitiesVersion` option should not be ignored if there is no custom capabilities file

### Features

* Close [#18](https://github.com/me-viper/OpaDotNet/issues/18). Add parameter to treat built-in function call errors as exceptions
* Close [#20](https://github.com/me-viper/OpaDotNet/issues/20). Support compilation from source string

## OpaDotNet.Wasm v1.3.0 (2023-08-09)

### Features

* [#1](https://github.com/me-viper/OpaDotNet/issues/1). Implements missing SDKs:
  * units.parse
  * units.parse_bytes
* [#17](https://github.com/me-viper/OpaDotNet/issues/17). Include error output in RegoCompilationException if compilation fails

## OpaDotNet.Wasm v1.2.1 (2023-07-27)

### Bug Fixes

* Compiler should provide absolute output path to opa cli.

## OpaDotNet.Extensions v1.0.3 (2023-07-27)

### Features

* Improve policy sources change tracking
* Update OpaDotNet.Wasm to v1.2.1

## OpaDotNet.Wasm v1.2.0 (2023-07-25)

### Bug Fixes

* Fixes [#13](https://github.com/me-viper/OpaDotNet/issues/13). Handle empty predicate evaluation
* Fixes [#14](https://github.com/me-viper/OpaDotNet/issues/14). Respect OutputPath when merging capabilities

### Features

* [#1](https://github.com/me-viper/OpaDotNet/issues/1). Implement missing SDKs:
  * json.patch
  * json.match_schema
  * json.verify_schema
  * regex.template_match
  * semver.is_valid
  * semver.compare
  * yaml.is_valid
  * yaml.marshal
  * yaml.unmarshal
* [#12](https://github.com/me-viper/OpaDotNet/issues/12). Factory implementations suitable for creating multiple evaluator instances
* [#16](https://github.com/me-viper/OpaDotNet/issues/16). Provide option to preserve build artifacts

### BREAKING CHANGES

* DefaultOpaImportsAbi.ValueCache property is no longer available. Use DefaultOpaImportsAbi.CacheGetOrAddValue instead
* OpaEvaluatorFactory have been redesigned (see [#12](https://github.com/me-viper/OpaDotNet/issues/12))

## OpaDotNet.Extensions v1.0.2 (2023-07-25)

### Features

* Add JsonOptions configuration ([af93889](https://github.com/me-viper/OpaDotNet.Extensions/commit/af93889905d96be1b5b4ecdd783b3258b2aa4376))

## OpaDotNet.Wasm [v1.1.0](https://github.com/me-viper/OpaDotNet/compare/v1.0.0...v1.1.0) (2023-07-13)

### Bug Fixes

* [#10](https://github.com/me-viper/OpaDotNet/issues/10). Allow returning null in ImportsABI.
* Make compilation options parameter optional.
* Resolve issue with built-ins returning rego sets.

### Features

* [#1](https://github.com/me-viper/OpaDotNet/issues/1). Implement SDK functions:
  * rand.intn
  * indexof_n
  * sprintf
  * strings.any_prefix_match
  * strings.any_suffix_match
  * regex.find_n
  * regex.replace
  * regex.split
  * base64url.encode_no_pad
  * hex.decode
  * hex.encode
  * urlquery.decode
  * urlquery.decode_object
  * urlquery.encode
  * urlquery.encode_object
  * io.jwt.encode_sign
  * io.jwt.encode_sign_raw
  * io.jwt.decode
  * io.jwt.decode_verify
  * io.jwt.verify_es256
  * io.jwt.verify_es384
  * io.jwt.verify_es512
  * io.jwt.verify_hs256
  * io.jwt.verify_hs384
  * io.jwt.verify_hs512
  * io.jwt.verify_ps256
  * io.jwt.verify_ps384
  * io.jwt.verify_ps512
  * io.jwt.verify_rs256
  * io.jwt.verify_rs384
  * io.jwt.verify_rs512
  * time.add_date
  * time.clock
  * time.date
  * time.diff
  * time.now_ns
  * time.parse_duration_ns
  * time.parse_rfc3339_ns
  * time.weekday
  * crypto.hmac.equal
  * crypto.hmac.md5
  * crypto.hmac.sha1
  * crypto.hmac.sha256
  * crypto.hmac.sha512
  * crypto.md5
  * crypto.sha1
  * crypto.sha256
  * net.cidr_contains_matches
  * net.cidr_expand
  * net.cidr_is_valid
  * net.cidr_merge
  * net.lookup_ip_addr
  * uuid.rfc4122
  * semver.compare
  * semver.is_valid
  * opa.runtime
  * trace
* Rego sets serialization
* Support OPA capabilities merging on compilation
