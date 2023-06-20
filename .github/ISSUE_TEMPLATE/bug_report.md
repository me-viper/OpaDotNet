---
name: Bug report
about: Create a report to help us improve
title: ''
labels: ''
assignees: ''

---

**Describe the bug**
A clear and concise description of what the bug is.

**Rego Policy source**

```rego
package example

default hello = false

hello {
    x := input.message
    x == data.world
}
```

**Evaluation code**

```csharp
var factory = new OpaEvaluatorFactory();

using var engine = factory.CreateWithJsonData(
    "simple.wasm",
    "{ \"world\": \"world\" }"
    );

var result = engine.EvaluatePredicate(inp);
```

**Expected behavior**
A clear and concise description of what you expected to happen.

**Version [e.g. x.y.z]**

**Additional context**
Add any other context about the problem here.
