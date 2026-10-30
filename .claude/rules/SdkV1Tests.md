---
paths:
  - "src/Wasm/tests/SdkV1/**"
  - "src/Wasm/gen/**"
---

# OPA compatibility test cases

- The YAML files in `src/Wasm/tests/SdkV1/v1/` are copied from OPA's upstream test suite
  (`https://github.com/open-policy-agent/opa/tree/main/v1/test/cases/testdata/v1`). Don't edit them by hand; sync
  with upstream using `./build/sync-opa-tests.ps1 [-Ref <tag|branch>]` (defaults to the OPA version in
  `src/Compilation/Interop/opa-native/go.mod`). The script stages the result and lists changed files.
- The source generator in `src/Wasm/gen` turns these YAML files into xUnit test cases.
- `src/Wasm/gen/ignore.yaml` lists the test cases to skip.
- If an upstream test case fails, fix the builtin implementation (`src/Wasm/src/DefaultOpaImportsAbi.*.cs` or
  `src/Wasm/src/GoCompat/`). If the behavior is unsupported or SDK-specific, exclude the test case instead: add a
  regex matching its name (`<dir>/<note>`) to an existing `reason` group in `ignore.yaml`, or create a new group:

  ```yaml
  - reason: 'Why these test cases are excluded'
    regex:
    - 'dir/note'
  ```

- To run only these tests:

  ```pwsh
  dotnet test src/Wasm/tests/OpaDotNet.Wasm.Tests.csproj -- --filter-class "*SdkV1Tests"
  ```
