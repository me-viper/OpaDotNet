# OPA Bundle Source

In this sample `OpaDotNet` will use policy compiled as OPA policy bundle.

## 1. Create policy and data files

Create policy file `quickstart/policy.rego` with the following contents:

[!code-rego[](~/snippets/quickstart/example.rego)]

Create data file `quickstart/data.json` with the following contents:

[!code-json[](~/snippets/quickstart/data.json)]

## 2. Compile policy bundle

In this example we will compile policy bundle manually, see [Compilation](~/articles/compilation/compilation.md) section for more details.

```sh
opa build -t wasm -b -e example/hello ./quickstart
```

`opa` CLI will produce `bundle.tar.gz` file.

## 3. Add usings

[!code-csharp[](~/snippets/Snippets.cs#Usings)]

## 4. The code

[!code-csharp[](~/snippets/Snippets.cs#EvalBundle)]
