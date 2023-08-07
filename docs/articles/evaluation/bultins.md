# Custom Built-in Functions

OpaDotNet can be extended with custom built-in functions.

OPA supports built-in functions for simple operations like string manipulation and arithmetic as well as more complex operations like JWT verification and executing HTTP requests. If you need to to extend OPA with custom built-in functions for use cases or integrations that are not supported out-of-the-box you can supply the function definitions when you prepare queries.

Using custom built-in functions involves providing a declaration and implementation. The declaration tells OPA the function’s type signature and the implementation provides the callback that OPA can execute during query evaluation.

## Implementation

Usually you will extend `DefaultOpaImportsAbi` class to add new built-in functions:

In the following sample we define 5 custom functions:

- `custom.zeroArgBuiltin`
- `custom.oneArgBuiltin`
- `custom.twoArgBuiltin`
- `custom.threeArgBuiltin`
- `custom.fourArgBuiltin`

> [!NOTE]
> OPA supports custom functions with up to four arguments.

[!code-csharp[](../../snippets/Builtins.cs#CustomBuiltinsImpl)]

## Declaration

To make OPA aware about custom built-ins we need add [capabilities](https://www.openpolicyagent.org/docs/latest/deployments/#capabilities) file containing the types for declarations and runtime objects passed to your implementation.

> [!IMPORTANT]
> Custom built-ins are supported only for policy bundles.

`capabilities.json`
[!code-json[](../../snippets/eval/capabilities.json)]

## Policy

`policy.rego`
[!code-rego[](../../snippets/eval/custom-builtins-policy.rego)]

## Compilation

Place policy and capabilities files into `bundle` directory.

> [!NOTE]
> If you use standalone `capabilities.json` file it will restrict the built-in functions that policies may depend (i.e. you will also need to add all functions
> your policy is using). If you just want to add your custom built-in without adding any restrictions you can specify `RegoCliCompilerOptions.CapabilitiesVersion` parameter. When this parameter is defined behind the scenes compiler will merge OPA capabilities of a specified version with your custom capabilities.

Next we need to compile policy bundle and make it aware about our custom built-ins:

[!code-csharp[](../../snippets/Builtins.cs#CustomBuiltinsCompile)]

## Execution

Now we can evaluate policies using custom built-ins:

[!code-csharp[](../../snippets/Builtins.cs#CustomBuiltinsEval)]

If you executed this code you the output would be:

```bash
hello
hello arg0
hello arg0 arg1
hello arg0 arg1 arg2
hello arg0 arg1 arg2 arg3
```
