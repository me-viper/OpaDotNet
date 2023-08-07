# WASM binary

In this sample `OpaDotNet` will use policy compiled as WASM binary.

## 1. Create policy

Create policy file `policy.rego` with the following contents:

[!code-rego[](../../snippets/eval/eval.rego)]

## 2. Compile policy module

In this example we will compile policy manually, see [Compilation](../Compilation.md) section for more details.

```sh
opa build -t wasm -e example/allow policy.rego
```

`opa` CLI will produce `bundle.tar.gz` file.

Extract `policy.wasm` from bundle `bundle.tar.gz`:

```sh
tar -zxvf bundle.tar.gz /policy.wasm
```

## 3. Add usings

[!code-csharp[](../../snippets/Snippets.cs#Usings)]

## 4. The code

[!code-csharp[](../../snippets/Snippets.cs#EvalWasm)]
