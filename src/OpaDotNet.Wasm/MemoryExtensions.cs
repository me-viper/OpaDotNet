using System.Text;

using Wasmtime;

namespace OpaDotNet.Wasm;

internal static class MemoryExtensions
{
    public static int WriteString(this Memory memory, long address, ReadOnlySpan<char> value, Encoding? encoding = null)
    {
        if (address < 0)
            throw new ArgumentOutOfRangeException(nameof(address));

        encoding ??= Encoding.UTF8;

        return encoding.GetBytes(value, memory.GetSpan(address, (int)Math.Min(int.MaxValue, memory.GetLength() - address)));
    }

    // public static void WriteBytes(this Memory memory, long address, ReadOnlySpan<byte> bytes)
    // {
    //     bytes.CopyTo(memory.GetSpan(address, bytes.Length));
    // }

    public static T? ReadNullTerminatedJson<T>(this Memory memory, long address, JsonSerializerOptions? options = null)
    {
        if (address < 0)
            throw new ArgumentOutOfRangeException(nameof(address));

        var slice = memory.GetSpan(address, (int)Math.Min(int.MaxValue, memory.GetLength() - address));
        var terminator = slice.IndexOf((byte)0);

        if (terminator == -1)
            throw new InvalidOperationException("string is not null terminated");

        return JsonSerializer.Deserialize<T>(slice[..terminator], options);
    }
}