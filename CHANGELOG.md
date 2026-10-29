# Changelog

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
