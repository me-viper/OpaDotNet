# Policy Capabilities

The capabilities define the built-in functions and other language features that policies may depend on.

OpaDotNet allows you to define OPA capabilities during compilation in three ways:

## CapabilitiesVersion option

Defines [capabilities](https://www.openpolicyagent.org/docs/latest/cli/#capabilities) used to validate policies against a specific version of OPA.

For example:

```csharp
var opts = new RegoCliCompilerOptions
{
    CapabilitiesVersion = "v0.22.0"
};

var compiler = new RegoCliCompiler(new OptionsWrapper<RegoCliCompilerOptions>(opts));
```

Is equivalent to the following command:

```bash
opa build -t wasm ./ --capabilities v0.22.0
```

## Custom capabilities file

Defines [capabilities](https://www.openpolicyagent.org/docs/latest/cli/#capabilities) file used to validate policies against.

```csharp
var compiler = new RegoCliCompiler();

await using var policy = await compiler.CompileBundle(
  "./policy-bundle",
  "example",
  "mycaps.json"
  );
```

> [!IMPORTANT]
> Capabilities file applicable only to policy bundles.

Is equivalent to the following command:

```bash
opa build -t wasm ./policy-bundle -e example --capabilities mycaps.json
```

## Merged capabilities

If you want to add capabilities (for example, custom built-in functions) atop of standard capabilities you can use both options described above:

```csharp
var opts = new RegoCliCompilerOptions
{
    CapabilitiesVersion = "v0.53.1"
};

var compiler = new RegoCliCompiler(new OptionsWrapper<RegoCliCompilerOptions>(opts));

await using var policy = await compiler.CompileBundle(
  "./policy-bundle",
  "example",
  "mycaps.json"
  );
```

In this case OpaDotNet will merge `v0.53.1` capabilities with capabilities defined in `mycaps.json` file.

> [!IMPORTANT]
> Merged capabilities applicable only to policy bundles.
