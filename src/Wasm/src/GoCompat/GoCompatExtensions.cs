using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OpaDotNet.Wasm.GoCompat;

internal static class GoCompatExtensions
{
    public static BigIntJson? ToJson(this byte[]? num)
    {
        return num == null ? null : new BigIntJson(new(num, true, true));
    }

    public static int[]? ToIntArray(this Oid? oid) => oid?.Value?.Split('.').Select(int.Parse).ToArray();

    public static byte[] GetX509Signature(this X509Certificate2 certificate)
    {
        var signedData = certificate.RawDataMemory;
        AsnDecoder.ReadSequence(
            signedData.Span,
            AsnEncodingRules.BER,
            out var offset,
            out var length,
            out _
            );

        var certificateSpan = signedData.Span[offset..(offset + length)];
        AsnDecoder.ReadSequence(
            certificateSpan,
            AsnEncodingRules.BER,
            out var tbsOffset,
            out var tbsLength,
            out _
            );

        var offsetSpan = certificateSpan[(tbsOffset + tbsLength)..];
        AsnDecoder.ReadSequence(
            offsetSpan,
            AsnEncodingRules.BER,
            out var algOffset,
            out var algLength,
            out _
            );

        return AsnDecoder.ReadBitString(
            offsetSpan[(algOffset + algLength)..],
            AsnEncodingRules.BER,
            out _,
            out _
            );
    }
}