using JetBrains.Annotations;

namespace OpaDotNet.Wasm.Internal.V10;

/// <summary>
/// OPA WASM ABI v1.0
/// </summary>
[PublicAPI]
internal interface IOpaExportsAbi
{
    /// <summary>
    /// Returns the address of a mapping of built-in function names to numeric identifiers that are required by the policy.
    /// </summary>
    /// <returns>The address of mapping</returns>
    nint Builtins();

    /// <summary>
    /// Allocates <see cref="size"/> bytes in the shared memory.
    /// </summary>
    /// <param name="size">Size to be allocated in bytes</param>
    /// <returns>The starting address of allocated memory</returns>
    nint Malloc(int size);

    /// <summary>
    /// Returns the address of a mapping of entrypoints to numeric identifiers that can be selected when evaluating the policy.
    /// </summary>
    /// <returns>Returns the address of a mapping of entrypoints</returns>
    nint Entrypoints();

    /// <summary>
    /// Frees a pointer.
    /// </summary>
    /// <param name="ptr">Address of the pointer</param>
    void Free(nint ptr);

    /// <summary>
    /// Parses the JSON serialized value starting at <see cref="ptr"/> of <see cref="size"/> bytes.
    /// The parsed value may refer to a null, boolean, number, string, array, or object value.
    /// </summary>
    /// <param name="ptr">Address of the parsed value</param>
    /// <param name="size">Size of the parse value</param>
    /// <returns>The address of the parsed value</returns>
    nint JsonParse(nint ptr, int size);

    /// <summary>
    /// Dumps the value referred to by <see cref="ptr"/> to a null-terminated JSON serialized string.
    /// Rego sets are serialized as JSON arrays. Non-string Rego object keys are serialized as strings.
    /// </summary>
    /// <param name="ptr">The address of value</param>
    /// <returns>Address of the start of the string</returns>
    nint JsonDump(nint ptr);

    /// <summary>
    /// Parses the JSON serialized value starting at <see cref="ptr"/> of <see cref="size"/> bytes.
    /// The parsed value may refer to a null, boolean, number, string, array, or object value.
    /// Rego set literals are supported.
    /// </summary>
    /// <param name="ptr">Address of the parsed value</param>
    /// <param name="size">Size of the parse value</param>
    /// <returns>Address of the parsed value</returns>
    nint ValueParse(nint ptr, int size);

    /// <summary>
    /// Dumps the value referred to by <see cref="ptr"/> to a null-terminated JSON serialized string.
    /// Rego sets are serialized using the literal syntax and non-string Rego object keys are not serialized as strings.
    /// </summary>
    /// <param name="ptr">Address of the value</param>
    /// <returns></returns>
    nint ValueDump(nint ptr);

    /// <summary>
    /// Add the value at the <paramref name="valuePtr"/> into the object
    /// referenced by <paramref name="baseValuePtr"/> at the given path.
    /// Existing values will be updated. On success the value at <paramref name="valuePtr"/> is no longer owned by the caller,
    /// it will be freed with the base value. The path value must be freed by the caller after use by calling
    /// opa_value_free. (The original path string passed to <see cref="JsonParse"/> or <see cref="ValueParse"/>
    /// to create the value must be freed by calling <see cref="Free"/>.) 
    /// If an error occurs the base value will remain unchanged.
    /// </summary>
    /// <param name="baseValuePtr">The address of initial value</param>
    /// <param name="pathValuePtr">
    /// The address of array with path elements.
    /// Must point to an array value with string keys (eg: <c>["a", "b", "c"]</c>)
    /// </param>
    /// <param name="valuePtr">The address of value to be set</param>
    /// <example>
    /// Base object <c>{"a": {"b": 123}}</c>, path <c>["a", "x", "y"]</c>,
    /// and value <c>{"foo": "bar"}</c>
    /// will yield <c>{"a": {"b": 123, "x": {"y": {"foo": "bar"}}}}</c>
    /// </example>
    /// <returns></returns>
    OpaResult ValueAddPath(nint baseValuePtr, nint pathValuePtr, nint valuePtr);

    /// <summary>
    /// Remove the value from the object referenced by <paramref name="baseValuePtr"/> at the given path.
    /// Values removed will be freed.
    /// The path value must be freed by the caller after use.
    /// </summary>
    /// <param name="baseValuePtr">The address of initial value</param>
    /// <param name="pathValuePtr">
    /// The address of array with path elements.
    /// Must point to an array value with string keys (eg: <c>["a", "b", "c"]</c>)
    /// </param>
    /// <returns></returns>
    OpaResult ValueRemovePath(nint baseValuePtr, nint pathValuePtr);

    /// <summary>
    /// Get the current heap pointer.
    /// </summary>
    /// <returns>Heap pointer</returns>
    nint HeapPrtGet();

    /// <summary>
    /// Set the heap pointer for the next evaluation.
    /// </summary>
    /// <param name="ptr">Heap pointer</param>
    void HeapPtrSet(nint ptr);

    /// <summary>
    /// Creates new evaluation context.
    /// </summary>
    /// <returns>The address of a newly allocated evaluation context</returns>
    nint ContextCreate();

    /// <summary>
    /// Set the input value to use during evaluation.
    /// This must be called before each <see cref="Eval"/> call.
    /// If the input value is not set before evaluation, references to the input document result
    /// produce no results (i.e., they are undefined.)
    /// </summary>
    /// <param name="contextPtr">Evaluation context address</param>
    /// <param name="inputPtr">Input value address</param>
    void ContextSetInput(nint contextPtr, nint inputPtr);

    /// <summary>
    /// Set the data value to use during evaluation.
    /// This should be called before each <see cref="Eval"/> call.
    /// If the data value is not set before evaluation, references to base data documents
    /// produce no results (i.e., they are undefined.)
    /// </summary>
    /// <param name="contextPtr">Evaluation context address</param>
    /// <param name="dataPtr">Data value address</param>
    /// <returns></returns>
    void ContextSetData(nint contextPtr, nint dataPtr);

    /// <summary>
    /// Set the entrypoint to evaluate. By default, entrypoint with id 0 is evaluated.
    /// </summary>
    /// <param name="contextPtr">Evaluation context address</param>
    /// <param name="entrypointId">Entrypoint ID</param>
    void ContextSetEntrypoint(nint contextPtr, int entrypointId);

    /// <summary>
    /// Get the result set produced by the evaluation process.
    /// </summary>
    /// <param name="contextPtr">Evaluation context address</param>
    /// <returns>The address of the result</returns>
    nint ContextGetResult(nint contextPtr);

    /// <summary>
    /// Policy evaluation method.
    /// </summary>
    /// <param name="contextPtr">Evaluation context.</param>
    /// <returns>
    /// The return value is reserved for future use.
    /// </returns>
    void Eval(nint contextPtr);
}