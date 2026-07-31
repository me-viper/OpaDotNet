using System.Formats.Tar;
using System.IO.Compression;
using System.Text;

using OpaDotNet.Compilation.Abstractions;

namespace OpaDotNet.Compilation.Tests;

public class BundleWriterTests
{
    private const string Content = "package test";

    private static byte[] WithBom(string content)
        => [..Encoding.UTF8.Preamble, ..Encoding.UTF8.GetBytes(content)];

    private static byte[] ReadEntry(MemoryStream bundle, string entryName)
    {
        bundle.Seek(0, SeekOrigin.Begin);

        using var gzip = new GZipStream(bundle, CompressionMode.Decompress, true);
        using var ms = new MemoryStream();

        gzip.CopyTo(ms);
        ms.Seek(0, SeekOrigin.Begin);

        using var tr = new TarReader(ms);

        while (tr.GetNextEntry() is { } entry)
        {
            if (!string.Equals(entry.Name, entryName, StringComparison.Ordinal))
                continue;

            using var entryMs = new MemoryStream();
            entry.DataStream!.CopyTo(entryMs);

            return entryMs.ToArray();
        }

        throw new InvalidOperationException($"Entry {entryName} not found in bundle");
    }

    [Fact]
    public async Task WriteEntryBytesStripsUtf8Bom()
    {
        using var ms = new MemoryStream();

        await using (var writer = new BundleWriter(ms))
            writer.WriteEntry(WithBom(Content), "policy.rego");

        var result = ReadEntry(ms, "/policy.rego");

        Assert.Equal(Content, Encoding.UTF8.GetString(result));
    }

    [Fact]
    public async Task WriteEntryBytesWithoutBomIsUnchanged()
    {
        using var ms = new MemoryStream();

        await using (var writer = new BundleWriter(ms))
            writer.WriteEntry(Encoding.UTF8.GetBytes(Content), "policy.rego");

        var result = ReadEntry(ms, "/policy.rego");

        Assert.Equal(Content, Encoding.UTF8.GetString(result));
    }

    [Fact]
    public async Task WriteEntryStreamStripsUtf8Bom()
    {
        using var ms = new MemoryStream();

        await using (var writer = new BundleWriter(ms))
        {
            using var source = new MemoryStream(WithBom(Content));
            writer.WriteEntry(source, "policy.rego");
        }

        var result = ReadEntry(ms, "/policy.rego");

        Assert.Equal(Content, Encoding.UTF8.GetString(result));
    }

    [Fact]
    public async Task WriteEntryStreamWithoutBomIsUnchanged()
    {
        using var ms = new MemoryStream();

        await using (var writer = new BundleWriter(ms))
        {
            using var source = new MemoryStream(Encoding.UTF8.GetBytes(Content));
            writer.WriteEntry(source, "policy.rego");
        }

        var result = ReadEntry(ms, "/policy.rego");

        Assert.Equal(Content, Encoding.UTF8.GetString(result));
    }
}
