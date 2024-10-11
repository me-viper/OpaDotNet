namespace OpaDotNet.Wasm.Validation;

internal interface IBundleSignatureValidator
{
    public IReadOnlySet<SignedFile> Validate(BundleSignatures signatures, SignatureValidationOptions options);
}