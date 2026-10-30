# Policy Capabilities

The capabilities define the built-in functions and other language features that policies may depend on.

OpaDotNet allows you to define OPA capabilities during compilation in three ways, all of them configured via
[CompilationParameters](xref:OpaDotNet.Compilation.Abstractions.CompilationParameters):

[!code-csharp[](~/snippets/Capabilities.cs#CapabilitiesUsings)]

## CapabilitiesVersion option

Defines [capabilities](https://www.openpolicyagent.org/docs/latest/cli/#capabilities) used to validate policies against a specific version of OPA.

For example:

[!code-csharp[](~/snippets/Capabilities.cs#CapabilitiesVersion)]

Is equivalent to the following command:

```bash
opa build -t wasm -b ./quickstart -e example/hello --capabilities v0.53.1
```

## Custom capabilities file

Defines [capabilities](https://www.openpolicyagent.org/docs/latest/cli/#capabilities) file used to validate policies against.

[!code-csharp[](~/snippets/Capabilities.cs#CapabilitiesFile)]

> [!IMPORTANT]
> Capabilities file applicable only to policy bundles.

> [!NOTE]
> Capabilities file used on its own replaces the whole list of available built-ins, so it must declare every built-in
> the policy uses, including operators like `=` (`eq`). To only add built-ins atop of standard ones use
> [merged capabilities](#merged-capabilities).

Is equivalent to the following command:

```bash
opa build -t wasm -b ./builtins -e custom_builtins/zero_arg --capabilities builtins/capabilities.json
```

## Merged capabilities

If you want to add capabilities (for example, custom built-in functions) atop of standard capabilities you can use both options described above:

[!code-csharp[](~/snippets/Capabilities.cs#CapabilitiesMerged)]

In this case OpaDotNet will merge `v0.53.1` capabilities with capabilities defined in `capabilities.json` file.

> [!IMPORTANT]
> Merged capabilities applicable only to policy bundles.
