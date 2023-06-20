using System.Formats.Tar;
using System.IO.Compression;

namespace OpaDotNet.Wasm;

internal static class TarGzHelper
{
    public static async Task<Stream?> GetFileAsync(
        Stream archive,
        Predicate<TarEntry> findFile,
        CancellationToken cancellationToken = default)
    {
        await using var gzip = new GZipStream(archive, CompressionMode.Decompress);
        using var ms = new MemoryStream();

        await gzip.CopyToAsync(ms, cancellationToken);
        ms.Seek(0, SeekOrigin.Begin);

        var tr = new TarReader(ms);

        while (await tr.GetNextEntryAsync(false, cancellationToken) is { } entry)
        {
            if (!findFile(entry))
                continue;

            if (entry.DataStream == null)
                throw new InvalidOperationException($"Failed to read {entry.Name}");

            var result = new MemoryStream();
            await entry.DataStream.CopyToAsync(result, cancellationToken);
            result.Seek(0, SeekOrigin.Begin);

            return result;
        }

        return null;
    }
}