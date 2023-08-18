# Policy Compilation

To evaluate policy modules they need to be compiled into WASM.

You can compile them manually with `opa build -t wasm ...` [command](https://www.openpolicyagent.org/docs/latest/cli/#opa-build) or ask OpaDotNet do that for you.

OpaDotNet provides two options for policy compilation:

- [`OpaDotNet.Compilation.Cli`](https://github.com/me-viper/OpaDotNet.Compilation/tree/main/src/OpaDotNet.Compilation.Cli) - wrapper over `opa` CLI [tool](https://www.openpolicyagent.org/docs/latest/cli).
- [`OpaDotNet.Compilation.Interop`](https://github.com/me-viper/OpaDotNet.Compilation/tree/main/src/OpaDotNet.Compilation.Interop) - wrapper over OPA SDK.

Which one you should be using?

Use `OpaDotNet.Compilation.Cli` if you have `opa` CLI tool available or you need functionality besides compilation (running tests, syntax checking etc.). Suitable for web applications and/or applications running in Docker containers. See [README](https://github.com/me-viper/OpaDotNet.Compilation/blob/main/src/OpaDotNet.Compilation.Cli/README.md) for more details.

Use `OpaDotNet.Compilation.Interop` if you need compilation only and want to avoid having external dependencies. Suitable for libraries, console application etc. See [README](https://github.com/me-viper/OpaDotNet.Compilation/blob/main/src/OpaDotNet.Compilation.Interop/README.md) for more details.
