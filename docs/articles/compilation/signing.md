# Bundle Signature Validation

OPA bundles can be [signed](https://www.openpolicyagent.org/docs/latest/management-bundles/#signing) to protect
against tampering. A signature is a JWT (stored as `.signatures.json` in the bundle) that lists every file in the
bundle together with its hash. `OpaDotNet` can verify that signature while unpacking a bundle, before any of its
contents are loaded into the evaluator.

> [!NOTE]
> Signature validation only applies when loading a policy bundle (`OpaBundleEvaluatorFactory` /
> `OpaEvaluatorFactory.CreateFromBundle`). It has no effect when evaluating a raw compiled `policy.wasm` module.

## 1. Sign the bundle

Signing is done with the `opa` CLI, typically as part of your build pipeline:

```sh
opa build -t wasm -b -e example/hello --signing-alg RS256 --signing-key private_key.pem -o bundle.tar.gz ./quickstart
```

This embeds a `.signatures.json` file into the resulting bundle, signed with the private key. Distribute the
matching public key alongside your application so it can verify bundles at load time.

## 2. Configure validation

Signature validation is controlled by
[WasmPolicyEngineOptions.SignatureValidation](xref:OpaDotNet.Wasm.WasmPolicyEngineOptions#OpaDotNet_Wasm_WasmPolicyEngineOptions_SignatureValidation),
which is a [SignatureValidationOptions](xref:OpaDotNet.Wasm.SignatureValidationOptions):

[!code-csharp[](~/snippets/Snippets.cs#EvalSignedBundle)]

Key options:

- [Validation](xref:OpaDotNet.Wasm.SignatureValidationOptions#OpaDotNet_Wasm_SignatureValidationOptions_Validation) -
  one of [SignatureValidationType](xref:OpaDotNet.Wasm.SignatureValidationType):
  - `Default` - validate the signature if the bundle has one, skip otherwise.
  - `Required` - fail if the bundle is not signed.
  - `Skip` - never validate, even if a signature is present.
- [VerificationKey](xref:OpaDotNet.Wasm.SignatureValidationOptions#OpaDotNet_Wasm_SignatureValidationOptions_VerificationKey) /
  [VerificationKeyPath](xref:OpaDotNet.Wasm.SignatureValidationOptions#OpaDotNet_Wasm_SignatureValidationOptions_VerificationKeyPath) -
  the secret (HMAC) or PEM-encoded public key (RSA/ECDSA) used to verify the signature. `VerificationKeyPath` is
  ignored if `VerificationKey` is also specified.
- [VerificationKeyId](xref:OpaDotNet.Wasm.SignatureValidationOptions#OpaDotNet_Wasm_SignatureValidationOptions_VerificationKeyId) -
  expected key id (`kid`), matched against the token header when present (default `"default"`).
- [SigningAlgorithm](xref:OpaDotNet.Wasm.SignatureValidationOptions#OpaDotNet_Wasm_SignatureValidationOptions_SigningAlgorithm) -
  expected JWT signing algorithm (default `"RS256"`).
- [ExcludeFiles](xref:OpaDotNet.Wasm.SignatureValidationOptions#OpaDotNet_Wasm_SignatureValidationOptions_ExcludeFiles) -
  file names to exclude from verification (e.g. files added to the bundle after it was signed).

## What gets checked

While unpacking the bundle, `OpaDotNet`:

- Verifies the `.signatures.json` JWT signature and, if configured, its `kid`.
- Recomputes the hash of every file in the bundle and compares it against the hash recorded in the signature.
- Fails if a file present in the bundle is missing from the signature, or vice versa (unless the file is listed in
  `ExcludeFiles`).

Any failure raises an exception derived from `OpaRuntimeException`:

- [BundleSignatureValidationException](xref:OpaDotNet.Wasm.Validation.BundleSignatureValidationException) - general
  signature validation failures (missing/invalid key, JWT validation failure, structural mismatch between bundle and
  signature).
- [BundleChecksumValidationException](xref:OpaDotNet.Wasm.Validation.BundleChecksumValidationException) - a file's
  hash does not match the one recorded in the signature. Exposes `FileName`, `Alg`, `Expected` and `Actual` for
  diagnostics.
