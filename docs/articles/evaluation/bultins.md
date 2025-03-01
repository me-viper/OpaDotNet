# Custom Built-in Functions

OpaDotNet can be extended with custom built-in functions.

OPA supports built-in functions for simple operations like string manipulation and arithmetic as well as more complex operations like JWT verification and executing HTTP requests. If you need to extend OPA with custom built-in functions for use cases or integrations that are not supported out-of-the-box you can supply the function definitions when you prepare queries.

Using custom built-in functions involves providing a declaration and implementation. The declaration tells OPA the function’s type signature and the implementation provides the callback that OPA can execute during query evaluation.

## Implementation

Usually, you will extend `DefaultOpaImportsAbi` class to add new built-in functions:

In the following sample, we define 5 custom functions:

- `custom.zeroArgBuiltin`
- `custom.oneArgBuiltin`
- `custom.twoArgBuiltin`
- `custom.threeArgBuiltin`
- `custom.fourArgBuiltin`

> [!NOTE]
> OPA supports custom functions with up to four arguments.

# [v2.6+](#tab/v26)

[!code-csharp[](~/snippets/Builtins.cs#CustomBuiltinsImplV26)]

# [v2.5](#tab/v25)

[!code-csharp[](~/snippets/Builtins.cs#CustomBuiltinsImpl)]

---

## Declaration

To make OPA aware of custom built-ins we need to add [capabilities](https://www.openpolicyagent.org/docs/latest/deployments/#capabilities)](https://www.openpolicyagent.org/docs/latest/deployments/#capabilities) file containing the types for declarations and runtime objects passed to your implementation.

> [!IMPORTANT]
> Custom built-ins are supported only for policy bundles.

`capabilities.json`
[!code-json[](~/snippets/builtins/capabilities.json)]

For more information on capabilities [see](~/articles/reference/capabilities.md)

## Policy

`policy.rego`
[!code-rego[](~/snippets/builtins/custom-builtins-policy.rego)]

## Compilation

Place policy and capabilities files into `bundle` directory.

Next, we need to compile the policy bundle and make it aware of our custom built-ins:

# [v2.6+](#tab/v26)

[!code-csharp[](~/snippets/Builtins.cs#CustomBuiltinsCompileV26)]

# [v2.5](#tab/v25)

[!code-csharp[](~/snippets/Builtins.cs#CustomBuiltinsCompile)]

---

## Execution

Now we can evaluate policies using custom built-ins:

[!code-csharp[](~/snippets/Builtins.cs#CustomBuiltinsEval)]

If you executed this code the output would be:

```bash
hello
hello arg0
hello arg0 arg1
hello arg0 arg1 arg2
hello arg0 arg1 arg2 arg3
```
