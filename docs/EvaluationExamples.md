# Examples

## WASM binary

### 1. Create policy

Create policy file `policy.rego` with the following contents:

```rego
package example

import future.keywords.if

default allow := false

allow if {
    data.password == input.password
}
```

### 2. Compile policy module

In this example we will compile policy manually, see [Compilation](./Compilation.md) section for more details.

```sh
opa build -t wasm -e example/allow policy.rego
```

`opa` CLI should produce `bundle.tar.gz` file.

Unpack `bundle.tar.gz` and find policy.wasm file

```sh
tar -zxvf bundle.tar.gz /policy.wasm
```

### 3. Run the code

```csharp
var factory = new OpaEvaluatorFactory();

// Create evaluator from compiled policy module.
using var engine = factory.CreateFromWasm(File.OpenRead("policy.wasm"));

// Set external data.
var data = "{\"password\":\"pwd\"}";
engine.SetDataFromRawJson(data);

// Evaluate. Policy query will return false.
var policyResult1 = engine.EvaluatePredicate(new { password = "wrong!" });

if (policyResult1.Result)
{
    // Should not get here.
}
else
{
    // Wrong password.
}

// Evaluate. Policy query will return true.
var policyResult1 = engine.EvaluatePredicate(new { password = "pwd" });

if (policyResult1.Result)
{
    // Correct password.
}
else
{
    // Should not get here.
}
```

## OPA Bundle

### 1. Create policy and data files

Create policy file `policy.rego` with the following contents:

```rego
package example

import future.keywords.if

default allow := false

allow if {
    data.password == input.password
}
```

Create data file `data.json` with the following contents:

```json
{
    "password": "pwd"
}
```

### 2. Compile policy bundle

In this example we will compile policy bundle manually, see [Compilation](./Compilation.md) section for more details.

```sh
opa build -t wasm -b -e example/allow ./
```

`opa` CLI should produce `bundle.tar.gz` file.

### 3. Run the code

```csharp
var factory = new OpaEvaluatorFactory();

// Create evaluator from compiled policy module.
using var engine = factory.CreateFromBundle(File.OpenRead("bundle.tar.gz"));

// External data is in the bundle already.

// Evaluate. Policy query will return false.
var policyResult1 = engine.EvaluatePredicate(new { password = "wrong!" });

if (policyResult1.Result)
{
    // Should not get here.
}
else
{
    // Wrong password.
}

// Evaluate. Policy query will return true.
var policyResult1 = engine.EvaluatePredicate(new { password = "pwd" });

if (policyResult1.Result)
{
    // Correct password.
}
else
{
    // Should not get here.
}
```
