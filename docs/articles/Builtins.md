# Built-in Functions Implementation Status

Bellow is the list of supported OPA built-in functions as for version v0.53.1 along with their implementation status in OpaDotNet.Wasm.

## Numbers

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [numbers.range_step](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-numbers-numbersrange_step) | :white_check_mark: | v2.2.0 |

## Strings

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [indexof_n](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-strings-indexof_n)  | :white_check_mark: | v1.0.0 |
| [sprintf](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-strings-sprintf)    | :white_check_mark: | v1.1.0 (*) |
| [strings.strings.any_prefix_match](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-strings-stringsany_prefix_match)    | :white_check_mark: | v1.0.0 |
| [strings.any_suffix_match](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-strings-stringsany_suffix_match)    | :white_check_mark: | v1.0.0 |
| [strings.render_template](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-strings-stringsrender_template)    | - | - |

\* Inconsistent behavior with native implementation when argument is object.

## Time

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [time.add_date](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-time-timeadd_date) | :white_check_mark: | v1.0.0 |
| [time.clock](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-time-timeclock) | :white_check_mark: | v1.0.0 |
| [time.date](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-time-timedate) | :white_check_mark: | v1.0.0 |
| [time.diff](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-time-timediff) | :white_check_mark: | v1.0.0 |
| [time.format](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-time-timeformat) | - | - |
| [time.now_ns](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-time-timenow_ns) | :white_check_mark: | v1.0.0 |
| [time.parse_duration_ns](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-time-timeparse_duration_ns) | :white_check_mark: | v1.1.0 |
| [time.parse_ns](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-time-timeparse_ns) | - | - |
| [time.parse_rfc3339_ns](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-time-timeparse_rfc3339_ns) | :white_check_mark: | v1.1.0 |
| [time.weekday](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-time-timeweekday) | :white_check_mark: | v1.0.0 |

## JSON

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [json.match_schema](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-object-jsonmatch_schema) | :white_check_mark: | v1.2.0 (*) |
| [json.patch](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-object-jsonpatch) | :white_check_mark: | v1.2.0 |
| [json.verify_schema](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-object-jsonverify_schema) | :white_check_mark: | v1.2.0 |

\* Due to differences in JSON Schema libraries reported schema validation errors might differ from native.

## Types

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [object.subset](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-object-objectsubset) | :white_check_mark: | v2.5.0 |

## Regex

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [regex.find_n](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-regex-regexfind_n) | :white_check_mark: | v1.1.0 |
| [regex.globs_match](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-regex-regexglobs_match) | - | - |
| [regex.replace](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-regex-regexreplace) | :white_check_mark: | v1.1.0 |
| [regex.split](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-regex-regexsplit) | :white_check_mark: | v1.1.0 |
| [regex.template_match](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-regex-regextemplate_match) | :white_check_mark: | v1.2.0 |

## Glob

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [glob.quote_meta](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-glob-globquote_meta) | :white_check_mark: | v2.2.0 |

## Units

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [units.parse](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-units-unitsparse) | :white_check_mark: | v1.3.0 |
| [units.parse_bytes](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-units-unitsparse_bytes) | :white_check_mark: | v1.3.0 |

## Encoding

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [base64url.encode_no_pad](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-encoding-base64urlencode_no_pad) | :white_check_mark: | v1.1.0 |
| [hex.decode](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-encoding-hexdecode) | :white_check_mark: | v1.1.0 |
| [hex.encode](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-encoding-hexencode) | :white_check_mark: | v1.1.0 |
| [urlquery.decode](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-encoding-urlquerydecode) | :white_check_mark: | v1.1.0 |
| [urlquery.decode_object](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-encoding-urlquerydecode_object) | :white_check_mark: | v1.1.0 |
| [urlquery.encode](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-encoding-urlqueryencode) | :white_check_mark: | v1.1.0 |
| [urlquery.encode_object](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-encoding-urlqueryencode_object) | :white_check_mark: | v1.1.0 |

## JWT

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [io.jwt.encode_sign](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-tokensign-iojwtencode_sign) | :white_check_mark: | v1.1.0 |
| [io.jwt.encode_sign_raw](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-tokensign-iojwtencode_sign_raw) | :white_check_mark: | v1.1.0 |
| [io.jwt.decode](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-tokens-iojwtdecode) | :white_check_mark: | v1.1.0 |
| [io.jwt.decode_verify](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-tokens-iojwtdecode_verify) | :white_check_mark: | v1.1.0 |
| [io.jwt.verify_es256](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-tokens-iojwtverify_es256) | :white_check_mark: | v1.1.0 |
| [io.jwt.verify_es384](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-tokens-iojwtverify_es384) | :white_check_mark: | v1.1.0 |
| [io.jwt.verify_es512](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-tokens-iojwtverify_es512) | :white_check_mark: | v1.1.0 |
| [io.jwt.verify_hs256](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-tokens-iojwtverify_hs256) | :white_check_mark: | v1.1.0 |
| [io.jwt.verify_hs384](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-tokens-iojwtverify_hs384) | :white_check_mark: | v1.1.0 |
| [io.jwt.verify_hs512](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-tokens-iojwtverify_hs512) | :white_check_mark: | v1.1.0 |
| [io.jwt.verify_ps256](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-tokens-iojwtverify_ps256) | :white_check_mark: | v1.1.0 |
| [io.jwt.verify_ps384](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-tokens-iojwtverify_ps384) | :white_check_mark: | v1.1.0 |
| [io.jwt.verify_ps512](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-tokens-iojwtverify_ps512) | :white_check_mark: | v1.1.0 |
| [io.jwt.verify_rs256](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-tokens-iojwtverify_rs256) | :white_check_mark: | v1.1.0 |
| [io.jwt.verify_rs384](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-tokens-iojwtverify_rs384) | :white_check_mark: | v1.1.0 |
| [io.jwt.verify_rs512](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-tokens-iojwtverify_rs512) | :white_check_mark: | v1.1.0 |

## Cryptography

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [crypto.hmac.equal](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-crypto-cryptohmacequal) | :white_check_mark: | v1.1.0 |
| [crypto.hmac.md5](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-crypto-cryptohmacmd5) | :white_check_mark: | v1.1.0 |
| [crypto.hmac.sha1](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-crypto-cryptohmacsha1) |:white_check_mark: | v1.1.0 |
| [crypto.hmac.sha256](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-crypto-cryptohmacsha256) | :white_check_mark: | v1.1.0 |
| [crypto.hmac.sha512](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-crypto-cryptohmacsha512) | :white_check_mark: | v1.1.0 |
| [crypto.md5](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-crypto-cryptomd5) | :white_check_mark: | v1.1.0 |
| [crypto.sha1](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-crypto-cryptosha1) | :white_check_mark: | v1.1.0 |
| [crypto.sha256](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-crypto-cryptosha256) | :white_check_mark: | v1.1.0 |
| [crypto.x509.parse_and_verify_certificates](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-crypto-cryptox509parse_and_verify_certificates) | :white_check_mark: | v2.5.0 (*) |
| [crypto.x509.parse_certificate_request](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-crypto-cryptox509parse_certificate_request) | :white_check_mark: | v2.5.0 (*) |
| [crypto.x509.parse_certificates](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-crypto-cryptox509parse_certificates) | :white_check_mark: | v2.5.0 (*) |
| [crypto.x509.parse_keypair](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-crypto-cryptox509parse_keypair) | :white_check_mark: | v2.5.0 (*) |
| [crypto.x509.parse_rsa_private_key](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-crypto-cryptox509parse_rsa_private_key) | :white_check_mark: | v2.5.0 (*) |

\* Due to differences in libraries output might differ from native.

## Graphs

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [graph.reachable_paths](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-graph-graphreachable_paths) | - | - |

## GraphQL

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [graphql.is_valid](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-graphql-graphqlis_valid) | - | - |
| [graphql.parse](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-graphql-graphqlparse) | - | - |
| [graphql.parse_and_verify](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-graphql-graphqlparse_and_verify) | - | - |
| [graphql.parse_query](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-graphql-graphqlparse_query) | - | - |
| [graphql.parse_schema](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-graphql-graphqlparse_schema) | - | - |
| [graphql.schema_is_valid](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-graphql-graphqlschema_is_valid) | - | - |

## HTTP

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [http.send](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-http-httpsend) | - | - |

## AWS

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [providers.aws.sign_req](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-providersaws-providersawssign_req) | - | - |

## Net

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [net.cidr_contains_matches](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-net-netcidr_contains_matches) | :white_check_mark: | v1.1.0 |
| [net.cidr_expand](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-net-netcidr_expand) | :white_check_mark: | v1.1.0 |
| [net.cidr_is_valid](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-net-netcidr_is_valid) | :white_check_mark: | v1.1.0 |
| [net.cidr_merge](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-net-netcidr_merge) | :white_check_mark: | v1.1.0 |
| [net.lookup_ip_addr](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-net-netlookup_ip_addr) | :white_check_mark: | v1.1.0 (*) |

\* There might be inconsistent behavior with native implementation due to different DNS resolver.

## UUID

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [uuid.parse](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-uuid-uuidparse) | :white_check_mark: | v2.5.0 |
| [uuid.rfc4122](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-uuid-uuidrfc4122) | :white_check_mark: | v1.0.0 |

## SemVer

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [semver.compare](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-semver-semvercompare) | :white_check_mark: | v1.2.0 |
| [semver.is_valid](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-semver-semveris_valid) | :white_check_mark: | v1.2.0 |

## Rego

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [rego.metadata.chain](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-rego-regometadatachain) | - | - |
| [rego.metadata.rule](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-rego-regometadatarule) | - | - |
| [rego.parse_module](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-rego-regoparse_module) | - | - |

## OPA

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [opa.runtime](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-opa-oparuntime) | :white_check_mark: | v1.1.0 |

## Tracing

| Function   | Status             | OpaDotNet version  |
|------------|--------------------|--------------------|
| [trace](https://www.openpolicyagent.org/docs/latest/policy-reference/#builtin-tracing-tr)  | :white_check_mark: | v1.1.0 |

## Yaml

| Function        | Status             | OpaDotNet version  |
|-----------------|--------------------|--------------------|
| yaml.is_valid   | :white_check_mark: | v1.2.0 |
| yaml.marshal    | :white_check_mark: | v1.2.0 |
| yaml.unmarshal  | :white_check_mark: | v1.2.0 |
