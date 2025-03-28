using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OpaDotNet.Wasm.Rego;

internal ref struct OpaJsonReader
{
    public const int MaxDepth = 64;

    private OpaTokenEnumerator _enumerator;

    private int _depth;

    private InObjectStack _objStack;

    // ReSharper disable once ConvertToPrimaryConstructor
    public OpaJsonReader(ReadOnlySpan<char> json)
    {
        _enumerator = new(json);
    }

    public OpaJsonToken Token { get; private set; }

    private bool IsInArray() => _objStack[_depth] == ObjectType.Array;

    private bool IsInObject() => _objStack[_depth] == ObjectType.Object;

    private bool IsInSet() => _objStack[_depth] == ObjectType.Set;

    private void EnterObject(ObjectType type)
    {
        if (_depth++ > MaxDepth)
            throw new InvalidOperationException("Maximum depth reached");

        _objStack[_depth] = type;
    }

    private ObjectType LeaveObject()
    {
        var result = _objStack[_depth];
        _objStack[_depth] = ObjectType.None;
        _depth--;
        return result;
    }

    public bool Read()
    {
        if (IsInArray())
        {
            if (!ReadArray())
                return false;
        }
        else if (IsInSet())
        {
            if (!ReadSet())
                return false;
        }
        else if (IsInObject())
        {
            if (!ReadObject())
                return false;
        }
        else
        {
            if (!_enumerator.MoveNext())
                return false;

            if (_enumerator.Current.TokenType is OpaJsonTokenType.Colon or OpaJsonTokenType.Coma)
            {
                if (!_enumerator.MoveNext())
                    return false;
            }

            Token = _enumerator.Current;
        }

        if (Token.TokenType == OpaJsonTokenType.ArrayStart)
        {
            var next = _enumerator.PeekNext();

            // Special case when set is represented by the: [{"__rego_set":[1, 2, ...]}] array.
            if (next.TokenType == OpaJsonTokenType.ObjectStart)
            {
                var next2 = _enumerator.PeekNext(2);

                if (next2.TokenType == OpaJsonTokenType.String && next2.Buf is "__rego_set")
                {
                    EnterObject(ObjectType.Set);

                    // We are here: -->[<--{"__rego_set":[1, 2, ...]}]
                    // We want to be here: [{"__rego_set":-->[<--1, 2, ...]}]
                    SkipUntilOrFail(OpaJsonTokenType.ArrayStart);

                    Token = new(OpaJsonTokenType.SetStart, _enumerator.Current.Buf);

                    return true;
                }
            }

            EnterObject(ObjectType.Array);
            return true;
        }

        if (Token.TokenType == OpaJsonTokenType.ObjectStart)
        {
            var next = _enumerator.PeekNext();

            // Empty object: {}
            if (next.TokenType == OpaJsonTokenType.ObjectEnd)
            {
                EnterObject(ObjectType.Object);
                return true;
            }

            if (next.TokenType != OpaJsonTokenType.String)
            {
                EnterObject(ObjectType.Set);
                Token = new(OpaJsonTokenType.SetStart, Token.Buf);
                return true;
            }

            if (_enumerator.PeekNext(2).TokenType == OpaJsonTokenType.Colon)
                EnterObject(ObjectType.Object);
            else
            {
                EnterObject(ObjectType.Set);
                Token = new(OpaJsonTokenType.SetStart, Token.Buf);
            }

            return true;
        }

        if (Token.TokenType == OpaJsonTokenType.ArrayEnd)
        {
            var o = LeaveObject();

            if (o == ObjectType.Set)
            {
                Token = new(OpaJsonTokenType.SetEnd, Token.Buf);

                // We are here: [{"__rego_set":[1, 2, ...-->]<--}]
                // We want to be here: [{"__rego_set":[1, 2, ...]}-->]<--
                SkipUntilOrFail(OpaJsonTokenType.ArrayEnd);
            }

            return true;
        }

        if (Token.TokenType == OpaJsonTokenType.ObjectEnd)
        {
            var o = LeaveObject();

            if (o == ObjectType.Set)
                Token = new(OpaJsonTokenType.SetEnd, Token.Buf);

            return true;
        }

        return true;
    }

    private void SkipUntilOrFail(OpaJsonTokenType token)
    {
        while (_enumerator.MoveNext())
        {
            if (_enumerator.Current.TokenType == token)
                return;
        }

        throw new InvalidOperationException("Unexpected end of json");
    }

    private void MoveNextOrFail()
    {
        if (!_enumerator.MoveNext())
            throw new InvalidOperationException("Unexpected end of json");
    }

    private bool ReadArray()
    {
        if (!_enumerator.MoveNext())
            return false;

        if (_enumerator.Current.TokenType == OpaJsonTokenType.ArrayEnd)
        {
            Token = _enumerator.Current;
            return true;
        }

        while (true)
        {
            if (_enumerator.Current.TokenType is OpaJsonTokenType.Colon or OpaJsonTokenType.Coma)
            {
                MoveNextOrFail();
                continue;
            }

            Token = _enumerator.Current;
            return true;
        }
    }

    private bool ReadSet()
    {
        if (!_enumerator.MoveNext())
            return false;

        if (_enumerator.Current.TokenType == OpaJsonTokenType.ObjectEnd)
        {
            Token = new(OpaJsonTokenType.ObjectEnd);
            return true;
        }

        while (true)
        {
            if (_enumerator.Current.TokenType is OpaJsonTokenType.Colon or OpaJsonTokenType.Coma)
            {
                MoveNextOrFail();
                continue;
            }

            Token = _enumerator.Current;
            return true;
        }
    }

    private bool ReadObject()
    {
        if (!_enumerator.MoveNext())
            return false;

        if (_enumerator.Current.TokenType == OpaJsonTokenType.ObjectEnd)
        {
            Token = _enumerator.Current;
            return true;
        }

        var readingProperty = false;

        if (_enumerator.Current.TokenType == OpaJsonTokenType.String)
        {
            var nextToken = _enumerator.PeekNext();

            if (nextToken.TokenType == OpaJsonTokenType.Colon)
                readingProperty = true;
        }

        while (true)
        {
            if (_enumerator.Current.TokenType == OpaJsonTokenType.Colon)
            {
                MoveNextOrFail();
                readingProperty = false;
                continue;
            }

            if (_enumerator.Current.TokenType == OpaJsonTokenType.Coma)
            {
                MoveNextOrFail();
                readingProperty = true;
                continue;
            }

            if (readingProperty)
            {
                Token = new(OpaJsonTokenType.PropertyName, _enumerator.Current.Buf);
                return true;
            }

            Token = _enumerator.Current;
            return true;
        }
    }

    private enum ObjectType : short
    {
        None,
        Array,
        Object,
        Set,
    }

    [InlineArray(MaxDepth)]
    private struct InObjectStack
    {
        private ObjectType _el0;
    }

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    private ref struct OpaTokenEnumerator
    {
        private ReadOnlySpan<char> _json;

        private OpaJsonToken _token;

        private int _pos;

        public OpaJsonToken Current => _token;

        private string DebuggerDisplay => $"{_token.DebuggerDisplay}, Pos = {_pos}";

        // ReSharper disable once ConvertToPrimaryConstructor
        public OpaTokenEnumerator(ReadOnlySpan<char> json)
        {
            _json = json;
            _token = new(OpaJsonTokenType.Eof, ReadOnlySpan<char>.Empty);
        }

        public OpaJsonToken PeekNext(int n = 1)
        {
            var token = _token;
            var json = _json;

            for (var i = 0; i < n; i++)
            {
                if (token.Pos.Start.Value >= json.Length)
                    return new(OpaJsonTokenType.Eof);

                json = json[token.Pos];

                if (json.Length == 0)
                    return new(OpaJsonTokenType.Eof);

                token = NextPrimitiveToken(json);
            }

            return token;
        }

        public bool MoveNext()
        {
            if (_token.Pos.Start.Value >= _json.Length)
                return false;

            _json = _json[_token.Pos];

            if (_json.Length == 0)
                return false;

            _token = NextPrimitiveToken(_json);
            _pos += _token.Pos.Start.Value;

            return true;
        }

        private void Skip(int n = 1)
        {
            _json = _json[n..];
            _pos += n;
        }

        private OpaJsonToken NextPrimitiveToken(ReadOnlySpan<char> json)
        {
            for (var i = 0; i < json.Length; i++)
            {
                switch (json[i])
                {
                    case 'n':
                        return new(OpaJsonTokenType.Null, 4);
                    case 't':
                        return new(OpaJsonTokenType.Value, "true");
                    case 'f':
                        return new(OpaJsonTokenType.Value, "false");
                    case 's':
                        return new(OpaJsonTokenType.EmptySet, "set()");
                    case '"':
                        return ReadString(json[i..]);
                    case '{':
                        return new(OpaJsonTokenType.ObjectStart);
                    case '}':
                        return new(OpaJsonTokenType.ObjectEnd);
                    case '[':
                        if (json[i..].StartsWith("""[{"__rego_set":[]}]"""))
                            return new(OpaJsonTokenType.EmptySet, """[{"__rego_set":[]}]""".Length);

                        return new(OpaJsonTokenType.ArrayStart);
                    case ']':
                        return new(OpaJsonTokenType.ArrayEnd);
                    case ',':
                        return new(OpaJsonTokenType.Coma);
                    case ':':
                        return new(OpaJsonTokenType.Colon);
                    case var ws when char.IsWhiteSpace(ws):
                        Skip();
                        break;
                    default:
                        if (char.IsDigit(json[i]) || json[i] == '-')
                            return ReadNumber(json[i..]);

                        throw new InvalidOperationException($"Unexpected symbol {json[i]} at pos {_pos}");
                }
            }

            return new(OpaJsonTokenType.Eof, 0);
        }

        private OpaJsonToken ReadString(ReadOnlySpan<char> buf)
        {
            // buf[0] is '"'
            var pos = 1;
            var escaping = false;

            foreach (var ch in buf[1..])
            {
                switch (ch)
                {
                    case '\\':
                        escaping = true;
                        break;
                    case '"' when !escaping:
                        return new OpaJsonToken(OpaJsonTokenType.String, buf[1..pos], 2);
                    default:
                        escaping = false;
                        break;
                }

                pos++;
            }

            throw new InvalidOperationException($"Invalid string at pos {_pos + pos}");
        }

        private static readonly char[] ValidDigitChars = ['e', 'E', '.', '-', '+'];

        private static OpaJsonToken ReadNumber(ReadOnlySpan<char> buf)
        {
            var pos = 0;

            foreach (var ch in buf)
            {
                var isDigit = false;

                if (char.IsDigit(ch))
                {
                    pos++;
                    continue;
                }

                foreach (var vdc in ValidDigitChars)
                {
                    if (vdc == ch)
                    {
                        pos++;
                        isDigit = true;
                        break;
                    }
                }

                if (!isDigit)
                    break;
            }

            return new(OpaJsonTokenType.Value, buf[..pos]);
        }
    }
}