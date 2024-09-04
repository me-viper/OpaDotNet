using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

namespace OpaDotNet.Wasm.Internal;

// Borrowed from https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/System/Text/ValueStringBuilder.cs
internal ref struct ValueStringBuilder
{
    private char[]? _arrayToReturnToPool;

    private Span<char> _chars;

    private int _pos;

    public ValueStringBuilder(Span<char> buffer)
    {
        _arrayToReturnToPool = null;
        _chars = buffer;
        _pos = 0;
    }

    // public ValueStringBuilder(ReadOnlySpan<char> initialBuffer)
    // {
    //     _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(initialBuffer.Length);
    //     _chars = _arrayToReturnToPool;
    //     initialBuffer.CopyTo(_chars[..]);
    //     _pos = initialBuffer.Length;
    // }

    public ValueStringBuilder(int initialCapacity)
    {
        _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(initialCapacity);
        _chars = _arrayToReturnToPool;
        _pos = 0;
    }

    public int Length
    {
        get => _pos;
        set => _pos = value;
    }

    public int Capacity => _chars.Length;

    public ref char this[int index]
    {
        get => ref _chars[index];
    }

    public override string ToString()
    {
        var s = _chars[.._pos].ToString();
        Dispose();
        return s;
    }

    /// <summary>Returns the underlying storage of the builder.</summary>
    public Span<char> RawChars => _chars;

    public ReadOnlySpan<char> AsSpan() => _chars[.._pos];

    public ReadOnlySpan<char> AsSpan(int start) => _chars.Slice(start, _pos - start);

    public ReadOnlySpan<char> AsSpan(int start, int length) => _chars.Slice(start, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char c)
    {
        var pos = _pos;
        var chars = _chars;

        if ((uint)pos < (uint)chars.Length)
        {
            chars[pos] = c;
            _pos = pos + 1;
        }
        else
        {
            Grow(1);
            Append(c);
        }
    }

    public void Append(int n)
    {
        Append(n, ReadOnlySpan<char>.Empty);
    }

    public void Append(int n, ReadOnlySpan<char> format)
    {
        var len = (int)Math.Floor(Math.Log10(Math.Abs(n))) + 1;
        var pos = _pos;

        if (pos > _chars.Length - len)
            Grow(len);

        n.TryFormat(_chars[_pos..], out var bw, format, CultureInfo.InvariantCulture);
        _pos += bw;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string? s)
    {
        if (s == null)
            return;

        var pos = _pos;

        // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
        if (s.Length == 1 && (uint)pos < (uint)_chars.Length)
        {
            _chars[pos] = s[0];
            _pos = pos + 1;
        }
        else
        {
            AppendSlow(s);
        }
    }

    private void AppendSlow(string s)
    {
        var pos = _pos;

        if (pos > _chars.Length - s.Length)
            Grow(s.Length);

        s.CopyTo(_chars[pos..]);
        _pos += s.Length;
    }

    public void AppendQuoted(scoped ReadOnlySpan<char> value)
    {
        var pos = _pos;

        if (pos > _chars.Length - value.Length - 2)
        {
            Grow(value.Length + 2);
        }

        _chars[_pos] = '"';
        _pos++;

        value.CopyTo(_chars[_pos..]);
        _pos += value.Length;

        _chars[_pos] = '"';
        _pos++;
    }

    public void Append(scoped ReadOnlySpan<char> value)
    {
        var pos = _pos;

        if (pos > _chars.Length - value.Length)
        {
            Grow(value.Length);
        }

        value.CopyTo(_chars[_pos..]);
        _pos += value.Length;
    }

    [UsedImplicitly]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        var toReturn = _arrayToReturnToPool;

        // for safety, to avoid using pooled array if this instance is erroneously appended to again
        this = default;

        if (toReturn != null)
            ArrayPool<char>.Shared.Return(toReturn);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalCapacityBeyondPos)
    {
        if (_arrayToReturnToPool == null)
            throw new InvalidOperationException("Have been initialized from Span and can't grow");

        const uint arrayMaxLength = 0x7FFFFFC7; // same as Array.MaxLength

        // Increase to at least the required size (_pos + additionalCapacityBeyondPos), but try
        // to double the size if possible, bounding the doubling to not go beyond the max array length.
        var newCapacity = (int)Math.Max(
            (uint)(_pos + additionalCapacityBeyondPos),
            Math.Min((uint)_chars.Length * 2, arrayMaxLength)
            );

        // Make sure to let Rent throw an exception if the caller has a bug and the desired capacity is negative.
        // This could also go negative if the actual required length wraps around.
        var poolArray = ArrayPool<char>.Shared.Rent(newCapacity);

        _chars[.._pos].CopyTo(poolArray);

        var toReturn = _arrayToReturnToPool;

        _chars = poolArray;
        _arrayToReturnToPool = poolArray;

        if (toReturn != null)
            ArrayPool<char>.Shared.Return(toReturn);
    }
}