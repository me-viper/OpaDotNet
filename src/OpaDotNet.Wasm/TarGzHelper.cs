using System.Formats.Tar;
using System.IO.Compression;

namespace OpaDotNet.Wasm;

internal static class TarGzHelper
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
                throw new OpaRuntimeException($"Failed to read tar entry {entry.Name}");

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
            throw new OpaRuntimeException("Bundle does not contain policy.wasm file");

        return new(policy.Value, data ?? Memory<byte>.Empty);
    }

    public static DirectoryInfo UnpackBundle(Stream archive, DirectoryInfo basePath)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentNullException.ThrowIfNull(basePath);

        using var gzip = new GZipStream(archive, CompressionMode.Decompress);
        using var tr = new TarReader(gzip);

        var result = new DirectoryInfo(Path.Combine(basePath.FullName, Guid.NewGuid().ToString()));
        result.Create();

        while (tr.GetNextEntry() is { } entry)
        {
            if (entry.DataStream == null || entry.EntryType != TarEntryType.RegularFile)
                continue;

            // Do we care about other files?
            if (entry.Name.IndexOf('\\') > 0)
                continue;

            entry.ExtractToFile(Path.Combine(result.FullName, entry.Name.Trim('/')), true);
        }

        return result;
    }

    // public static async Task<OpaPolicy> ReadBundleAsync(
    //     Stream archive,
    //     CancellationToken cancellationToken = default)
    // {
    //     await using var gzip = new GZipStream(archive, CompressionMode.Decompress);
    //     using var ms = new MemoryStream();
    //
    //     await gzip.CopyToAsync(ms, cancellationToken);
    //     ms.Seek(0, SeekOrigin.Begin);
    //
    //     var tr = new TarReader(ms);
    //     
    //     static async Task<Stream> ReadEntry(TarEntry entry, CancellationToken cancellationToken)
    //     {
    //         if (entry.DataStream == null)
    //             throw new InvalidOperationException($"Failed to read {entry.Name}");
    //
    //         var result = new MemoryStream();
    //         await entry.DataStream.CopyToAsync(result, cancellationToken);
    //         result.Seek(0, SeekOrigin.Begin);
    //         
    //         return result;
    //     }
    //     
    //     Stream? policy = null;
    //     Stream? data = null;
    //     
    //     while (await tr.GetNextEntryAsync(false, cancellationToken) is { } entry)
    //     {
    //         if (string.Equals(entry.Name, "/policy.wasm", StringComparison.OrdinalIgnoreCase))
    //             policy = await ReadEntry(entry, cancellationToken);
    //
    //         if (string.Equals(entry.Name, "/data.json", StringComparison.OrdinalIgnoreCase))
    //             data = await ReadEntry(entry, cancellationToken);
    //     }
    //     
    //     if (policy == null)
    //         throw new OpaRuntimeException("Bundle does not contain policy.wasm file");
    //
    //     return new(policy, data);
    // }
}