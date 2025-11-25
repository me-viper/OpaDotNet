using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

using OpaDotNet.Compilation.Abstractions;

using Xunit;

namespace OpaDotNet.InternalTesting;

[ExcludeFromCodeCoverage]
public static class AssertBundle
{
    public static void IsValid(Stream bundle, bool hasData = false)
    {
        var policy = TarGzHelper.ReadBundle(bundle);

        Assert.True(policy.Policy.Length > 0);

        if (hasData)
            Assert.True(policy.Data.Length > 0);
    }

    public static bool HasEntry(TarEntry entry, string fileName)
    {
        Assert.NotNull(entry);

        if (!string.Equals(entry.Name, fileName, StringComparison.Ordinal))
            return false;

        Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
        Assert.NotNull(entry.DataStream);
        Assert.True(entry.DataStream.Length > 0);

        return true;
    }

    public static bool HasNonEmptyData(TarEntry entry)
    {
        Assert.NotNull(entry);

        if (!string.Equals(entry.Name, "/data.json", StringComparison.Ordinal))
            return false;

        Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
        Assert.NotNull(entry.DataStream);
        Assert.True(entry.DataStream.Length > 0);

        var buf = new byte[entry.DataStream.Length];
        _ = entry.DataStream.Read(buf);

        if (Encoding.UTF8.GetString(buf).StartsWith("{}"))
            Assert.Fail("Expected non empty data.json");

        return true;
    }

    public static bool AssertManifest(TarEntry entry, Predicate<BundleManifest> inspector)
    {
        Assert.NotNull(entry);

        if (!string.Equals(entry.Name, "/.manifest", StringComparison.Ordinal))
            return false;

        Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
        Assert.NotNull(entry.DataStream);
        Assert.True(entry.DataStream.Length > 0);

        var buf = new byte[entry.DataStream.Length];
        _ = entry.DataStream.Read(buf);

        var manifest = JsonSerializer.Deserialize<BundleManifest>(buf);

        Assert.NotNull(manifest);

        return inspector(manifest);
    }

    public static bool AssertData(TarEntry entry, Predicate<JsonDocument> inspector)
    {
        Assert.NotNull(entry);

        if (!string.Equals(entry.Name, "/data.json", StringComparison.Ordinal))
            return false;

        Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
        Assert.NotNull(entry.DataStream);
        Assert.True(entry.DataStream.Length > 0);

        var buf = new byte[entry.DataStream.Length];
        _ = entry.DataStream.Read(buf);

        if (Encoding.UTF8.GetString(buf).StartsWith("{}"))
            Assert.Fail("Expected non empty data.json");

        var json = JsonDocument.Parse(buf);

        return inspector(json);
    }

    public static void DumpBundle(Stream bundle, ITestOutputHelper output)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        using var gzip = new GZipStream(bundle, CompressionMode.Decompress, true);
        using var ms = new MemoryStream();

        gzip.CopyTo(ms);
        ms.Seek(0, SeekOrigin.Begin);

        using var tr = new TarReader(ms);

        while (tr.GetNextEntry() is { } entry)
            output.WriteLine($"{entry.Name} [{entry.EntryType}]");

        bundle.Seek(0, SeekOrigin.Begin);
    }

    public static void Content(Stream bundle, params Predicate<TarEntry>[] inspectors)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        using var gzip = new GZipStream(bundle, CompressionMode.Decompress);
        using var ms = new MemoryStream();

        gzip.CopyTo(ms);
        ms.Seek(0, SeekOrigin.Begin);

        using var tr = new TarReader(ms);
        var entries = new List<TarEntry>();

        while (tr.GetNextEntry() is { } entry)
            entries.Add(entry);

        var i = 0;

        foreach (var inspector in inspectors)
        {
            var hasMatch = entries.Any(p => inspector(p));

            if (!hasMatch)
            {
                var content = string.Join(Environment.NewLine, entries.Select(p => p.Name));
                Assert.Fail($"Inspector at index {i} didn't match any entry in the bundle.\n{content}");
            }

            i++;
        }
    }
}

[ExcludeFromCodeCoverage]
public record OpaPolicy(ReadOnlyMemory<byte> Policy, ReadOnlyMemory<byte> Data);

[ExcludeFromCodeCoverage]
public static class TarGzHelper
{
    public static OpaPolicy ReadBundle(Stream archive)
    {
        ArgumentNullException.ThrowIfNull(archive);

        using var gzip = new GZipStream(archive, CompressionMode.Decompress);
        using var ms = new MemoryStream();

        gzip.CopyTo(ms);
        ms.Seek(0, SeekOrigin.Begin);

        using var tr = new TarReader(ms);

        static Memory<byte> ReadEntry(TarEntry entry)
        {
            if (entry.DataStream == null)
                throw new InvalidOperationException($"Failed to read {entry.Name}");

            var result = new byte[entry.DataStream.Length];
            var bytesRead = entry.DataStream.Read(result);

            if (bytesRead < entry.DataStream.Length)
                throw new Exception($"Failed to read tar entry {entry.Name}");

            return result;
        }

        Memory<byte>? policy = null;
        Memory<byte>? data = null;

        while (tr.GetNextEntry() is { } entry)
        {
            if (string.Equals(entry.Name, "/policy.wasm", StringComparison.OrdinalIgnoreCase))
                policy = ReadEntry(entry);

            if (string.Equals(entry.Name, "/data.json", StringComparison.OrdinalIgnoreCase))
                data = ReadEntry(entry);
        }

        if (policy == null)
            throw new Exception("Bundle does not contain policy.wasm file");

        return new(policy.Value, data ?? Memory<byte>.Empty);
    }
}