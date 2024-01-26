# Changelog

## OpaDotNet.Wasm v2.4.1 (2024-01-26)

### Bug Fixes

* [#26](https://github.com/me-viper/OpaDotNet/issues/26). time.now_ns() should return time in UTC ([2eb1190](https://github.com/me-viper/OpaDotNet/commit/2eb1190d3d5fede52e795e1dbcae44ca89cce831))
* [#27](https://github.com/me-viper/OpaDotNet/issues/27), [#28](https://github.com/me-viper/OpaDotNet/issues/28). Fix token validation. ([3833e2d](https://github.com/me-viper/OpaDotNet/commit/3833e2dd1096069c09d1a0d683ac725ac3150a5b))
* Fallback to default lifetime validation if time constraint is not set ([ac6210d](https://github.com/me-viper/OpaDotNet/commit/ac6210d6785f7963e13ad7ddb8ea4188d2d6968e))

## OpaDotNet.Wasm v2.4.0 (2024-01-10)

### Bug Fixes

* [#24](https://github.com/me-viper/OpaDotNet/issues/24). Fix strings.sprintf function to use JsonSerializerOptions ([e69d916](https://github.com/me-viper/OpaDotNet/commit/e69d916f25d6070a83732e46924eb77f432ea1f5))

### Features

* Update dependencies ([9158856](https://github.com/me-viper/OpaDotNet/commit/9158856d2de0415d68ecb37953307fa1d87d94c2))

## OpaDotNet.Wasm v2.3.0 (2023-11-21)

### Features

* [#23]. Support net8.0 ([9b7f79b](https://github.com/me-viper/OpaDotNet/commit/9b7f79bcfc51113691f9f753ad477cd58c692a89))

## OpaDotNet.Wasm v2.2.0 (2023-10-09)

### Bug Fixes

* print() does not respect json serialization options ([a47356f](https://github.com/me-viper/OpaDotNet/commit/a47356f76df68a6f36cdd8a54ff3b74def809bf8))

### Features

* [#1](https://github.com/me-viper/OpaDotNet/issues/1). Implement missing builtins ([8412dba](https://github.com/me-viper/OpaDotNet/commit/8412dba2f572a469e55b208519b7eecfa658b6ad))
  * glob.quote_meta
* [#21](https://github.com/me-viper/OpaDotNet/issues/21). Implement new buildins ([8ee6944](https://github.com/me-viper/OpaDotNet/commit/8ee6944dca0bb139f2db1a60fc2a7f95e2a7b286))
  * numbers.range_step
* Close [#22](https://github.com/me-viper/OpaDotNet/issues/22). Use JavaScriptEncoder.UnsafeRelaxedJsonEscaping by default ([aa9b962](https://github.com/me-viper/OpaDotNet/commit/aa9b962e04d67555763962fa85b4609f8852fe1f))

## OpaDotNet.Wasm v2.1.1 (2023-09-29)

### Features

* [#11](https://github.com/me-viper/OpaDotNet/issues/11). Implement print() ([274fcfb](https://github.com/me-viper/OpaDotNet/commit/274fcfb0ed3c0c481ab37e547045b5b44e927bf1))

## OpaDotNet.Wasm v2.1.0 (2023-98-28)

### Features

* Update dependencies ([c4d591b](https://github.com/me-viper/OpaDotNet/commit/c4d591bab62f83c3efa8be1833e6d9afbda2e130))

## OpaDotNet.Wasm v2.0.0 (2023-08-18)

### Features

* Migrate to the new compilation infrastructure ([1e3815a](https://github.com/me-viper/OpaDotNet/commit/1e3815aa10d592a0d17c7cb74fd0375993e20973))

### BREAKING CHANGES

* Compilation logic have been moved from OpaDotNet.Wasm to OpaDotNet.Compilation.Cli assembly

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
