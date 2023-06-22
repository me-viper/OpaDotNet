using System.Formats.Tar;
using System.IO.Compression;

namespace OpaDotNet.Wasm;

internal static class TarGzHelper
{
    public static OpaPolicy ReadBundle(Stream archive)
    {
        using var gzip = new GZipStream(archive, CompressionMode.Decompress);
        using var ms = new MemoryStream();

        gzip.CopyTo(ms);
        ms.Seek(0, SeekOrigin.Begin);

        var tr = new TarReader(ms);
        
        static Stream ReadEntry(TarEntry entry)
        {
            if (entry.DataStream == null)
                throw new InvalidOperationException($"Failed to read {entry.Name}");

            var result = new MemoryStream((int)entry.DataStream.Length);
            entry.DataStream.CopyToAsync(result);
            result.Seek(0, SeekOrigin.Begin);
            
            return result;
        }
        
        Stream? policy = null;
        Stream? data = null;
        
        while (tr.GetNextEntry() is { } entry)
        {
            if (string.Equals(entry.Name, "/policy.wasm", StringComparison.OrdinalIgnoreCase))
                policy = ReadEntry(entry);

            if (string.Equals(entry.Name, "/data.json", StringComparison.OrdinalIgnoreCase))
                data = ReadEntry(entry);
        }
        
        if (policy == null)
            throw new OpaRuntimeException("Bundle does not contain policy.wasm file");

        return new(policy, data);
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