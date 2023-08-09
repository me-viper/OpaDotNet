# Changelog

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
