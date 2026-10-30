# Engine Options

[WasmPolicyEngineOptions](xref:OpaDotNet.Wasm.WasmPolicyEngineOptions) controls how evaluators are created and how
they run policies. Pass it to an evaluator factory constructor or to the static `Create` methods. If you don't,
`WasmPolicyEngineOptions.Default` is used.

[!code-csharp[](~/snippets/EngineOptions.cs#EngineOptionsUsings)]

[!code-csharp[](~/snippets/EngineOptions.cs#EngineOptions)]

> [!IMPORTANT]
> Options become read-only once they have been used to create an evaluator: setting any property afterwards throws
> `InvalidOperationException`. You can still pass the same instance to several factories.

## Overview

| Option                                      | Default                  | Description                                                              |
|---------------------------------------------|--------------------------|--------------------------------------------------------------------------|
| [Timeout](#timeout)                         | none                     | Maximum duration of a single evaluation.                                 |
| [MinMemoryPages](#memory)                   | `2`                      | Initial size of the WASM memory, in 64 KiB pages.                        |
| [MaxMemoryPages](#memory)                   | unlimited                | Maximum size of the WASM memory, in 64 KiB pages.                        |
| [SerializationOptions](#json-serialization) | see below                | JSON options used for input, data and results.                           |
| [CachePath](#cachepath)                     | none (in memory)         | Directory used to keep the policy on disk instead of memory.             |
| [MaxAbiVersion](#maxabiversion)             | the policy's ABI version | Highest OPA WASM ABI version the evaluator will use.                     |
| `StrictBuiltinErrors`                       | `false`                  | See [Handling exceptions](../reference/builtins.md#handling-exceptions). |
| `SignatureValidation`                       | validate if signed       | See [Bundle Signature Validation](../compilation/signing.md).            |
| `ConfigureBuiltins(...)`                    | default built-ins        | See [Custom Built-in Functions](builtins.md).                            |

## Timeout

[Timeout](xref:OpaDotNet.Wasm.WasmPolicyEngineOptions#OpaDotNet_Wasm_WasmPolicyEngineOptions_Timeout) limits how long
a single `Evaluate*` call may run. It doesn't apply to `SetData*` calls. When the limit is reached, evaluation is
interrupted and `OpaEvaluationException` is thrown (see [Error Handling](errors.md)).

The timeout covers both the policy itself and the built-ins it calls. The policy is interrupted through Wasmtime epoch
interruption. Built-ins can observe the timeout through `BuiltinContext.CancellationToken`, and async built-ins are
cancelled through it.

Enabling a timeout adds a small overhead to every evaluation, so leave it unset if your policies and inputs are
trusted and bounded.

## Memory

OPA policies run in WebAssembly linear memory, which is allocated in 64 KiB pages.

- [MinMemoryPages](xref:OpaDotNet.Wasm.WasmPolicyEngineOptions#OpaDotNet_Wasm_WasmPolicyEngineOptions_MinMemoryPages)
  is the amount of memory every evaluator starts with.
- [MaxMemoryPages](xref:OpaDotNet.Wasm.WasmPolicyEngineOptions#OpaDotNet_Wasm_WasmPolicyEngineOptions_MaxMemoryPages)
  caps how far memory can grow. If it's not set, memory grows on demand up to the WebAssembly limit (4 GiB).

Memory grows to fit external data, input and evaluation results, and it is not shrunk afterwards. Setting
`MaxMemoryPages` protects the host from unexpectedly large input or data. Exceeding it during evaluation fails with
`OpaEvaluationException`; exceeding it in `SetData*` throws the underlying Wasmtime exception. Every evaluator has its own memory, so the total footprint is roughly
`evaluators × memory per evaluator`. Keep this in mind when you pool evaluators.

## JSON serialization

[SerializationOptions](xref:OpaDotNet.Wasm.WasmPolicyEngineOptions#OpaDotNet_Wasm_WasmPolicyEngineOptions_SerializationOptions)
are the `System.Text.Json` options used to serialize input and external data passed to `Evaluate*`/`SetData`, and to
deserialize results returned by `Evaluate*`. They don't affect `EvaluateRaw` and `SetDataFromRawJson`, which work with
JSON strings directly.

The defaults don't set a naming policy, so C# property names are sent as is. A `Message` property becomes
`input.Message`, which won't match `input.message` in Rego. Use
[DefaultWithJsonOptions](xref:OpaDotNet.Wasm.WasmPolicyEngineOptions.DefaultWithJsonOptions*)
to adjust the defaults:

[!code-csharp[](~/snippets/EngineOptions.cs#EngineSerializationInput)]

[!code-csharp[](~/snippets/EngineOptions.cs#EngineSerializationOptions)]

You can also assign your own `JsonSerializerOptions` instance to `SerializationOptions`. It's copied, and OpaDotNet's
internal converter for Rego sets is always added to the copy.

> [!NOTE]
> The default options use `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`, so non-ASCII characters are passed to the
> policy unescaped. Keep this encoder if you replace the options and your input contains such characters.

## CachePath

By default an evaluator factory keeps the policy WASM binary (and, for bundles, the unpacked bundle) in memory for its
whole lifetime. If you set
[CachePath](xref:OpaDotNet.Wasm.WasmPolicyEngineOptions#OpaDotNet_Wasm_WasmPolicyEngineOptions_CachePath), the factory
instead copies the policy into a new subdirectory of that path. Every `Create()` call then reads the policy from disk,
and for bundles unpacks and validates it again. The subdirectory is deleted when the factory is disposed.

[!code-csharp[](~/snippets/EngineOptions.cs#EngineCachePath)]

The directory must exist and be writable. Use `CachePath` only when you keep many large policies loaded and memory
matters more than evaluator creation time. It has no benefit for the static `Create` methods, because they dispose the
factory right after creating the evaluator.

## MaxAbiVersion

OPA policies are compiled for a specific [WASM ABI version](../ABI.md). The evaluator uses the version the policy was
built for unless
[MaxAbiVersion](xref:OpaDotNet.Wasm.WasmPolicyEngineOptions#OpaDotNet_Wasm_WasmPolicyEngineOptions_MaxAbiVersion)
is lower. A value higher than the policy's version is ignored. `IOpaEvaluator.AbiVersion` reports the version in use.

[!code-csharp[](~/snippets/EngineOptions.cs#EngineMaxAbiVersion)]

ABI-specific features, such as `IUpdateDataFeature` (ABI 1.3+), are available through `IOpaEvaluator.TryGetFeature`
only when the selected version supports them. Lowering the ABI version is mostly useful for working around a problem
with a newer ABI or for testing.
