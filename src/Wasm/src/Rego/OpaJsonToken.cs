using System.Diagnostics;

namespace OpaDotNet.Wasm.Rego;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
internal readonly ref struct OpaJsonToken(OpaJsonTokenType token, ReadOnlySpan<char> value, int offset = 0)
{
    public OpaJsonToken(OpaJsonTokenType token, int offset = 1) : this(token, ReadOnlySpan<char>.Empty, offset)
    {
    }

    public ReadOnlySpan<char> Buf { get; } = value;

    public Range Pos { get; } = Range.StartAt(value.Length + offset);

    public OpaJsonTokenType TokenType { get; } = token;

    internal string DebuggerDisplay => $"TokenType = {TokenType}, Value = {Buf.ToString()}";
}