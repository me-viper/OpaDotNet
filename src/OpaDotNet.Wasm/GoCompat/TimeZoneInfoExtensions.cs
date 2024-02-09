using System.Diagnostics.CodeAnalysis;

namespace OpaDotNet.Wasm.GoCompat;

internal static class TimeZoneInfoExtensions
{
    internal static readonly IReadOnlyDictionary<string, string> ZoneAbbreviations = new Dictionary<string, string>
    {
        { "SAST", "South Africa Standard Time" },
        { "CAT", "Sudan Standard Time" },
        { "WAT", "W. Central Africa Standard Time" },
        { "EAT", "E. Africa Standard Time" },
        { "GMT", "GMT Standard Time" },
        { "BST", "GMT Standard Time" },
        { "EET", "Middle East Standard Time" },
        { "EEST", "Middle East Standard Time" },
        { "HST", "Aleutian Standard Time" },
        { "HDT", "Aleutian Standard Time" },
        { "AKST", "Aleutian Standard Time" },
        { "AKDT", "Aleutian Standard Time" },
        { "EST", "US Eastern Standard Time" },
        { "EDT", "US Eastern Standard Time" },
        { "CST", "Central America Standard Time" },
        { "MST", "Mountain Standard Time" },
        { "MDT", "Mountain Standard Time" },
        { "PST", "Pacific Standard Time" },
        { "PDT", "Pacific Standard Time" },
        { "AST", "Atlantic Standard Time" },
        { "ADT", "Atlantic Standard Time" },
        { "NST", "Newfoundland Standard Time" },
        { "NDT", "Newfoundland Standard Time" },
        { "IST", "Israel Standard Time" },
        { "IDT", "Israel Standard Time" },
        { "PKT", "Pakistan Standard Time" },
        { "KST", "Korea Standard Time" },
        { "JST", "Tokyo Standard Time" },
        { "ACST", "Cen. Australia Standard Time" },
        { "ACDT", "Cen. Australia Standard Time" },
        { "AEST", "AUS Eastern Standard Time" },
        { "AEDT", "AUS Eastern Standard Time" },
        { "AWST", "W. Australia Standard Time" },
        { "CET", "Central European Standard Time" },
        { "CEST", "Central European Standard Time" },
        { "NZST", "New Zealand Standard Time" },
        { "NZDT", "New Zealand Standard Time" },
        { "MSK", "Russian Standard Time" },
    };

    private static bool TryLookupAbbr(string abbr, [MaybeNullWhen(false)] out TimeZoneInfo tz)
    {
        tz = null;

        if (!ZoneAbbreviations.TryGetValue(abbr, out var zoneId))
            return false;

        // This kinda has to succeed.
        var systemZone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);

        // This is technically not valid way to do a clone, but on linux passing systemZone.GetAdjustmentRules() here throws:
        // "The elements of the AdjustmentRule array must be in chronological order and must not overlap".
        // For our purposes we really care only about Id and BaseUtcOffset.
        tz = TimeZoneInfo.CreateCustomTimeZone(
            abbr,
            systemZone.BaseUtcOffset,
            systemZone.DisplayName,
            systemZone.StandardName
            );

        return true;
    }

    public static TimeSpan FindSystemTimeZoneUtcOffset(string zoneId)
    {
#if NET8_0_OR_GREATER
        if (ZoneAbbreviations.TryGetValue(zoneId, out var abbr))
            zoneId = abbr;

        if (TimeZoneInfo.TryFindSystemTimeZoneById(zoneId, out var result))
            return result.BaseUtcOffset;

        throw new TimeZoneNotFoundException($"The time zone ID '{zoneId}' was not found on the local computer.");
#else
        if (ZoneAbbreviations.TryGetValue(zoneId, out var abbr))
            zoneId = abbr;

        return TimeZoneInfo.FindSystemTimeZoneById(zoneId).BaseUtcOffset;
#endif
    }

    public static TimeZoneInfo FindSystemTimeZoneByIdOrAbbr(string zoneId)
    {
#if NET8_0_OR_GREATER
        TimeZoneInfo? result;

        if (TimeZoneInfo.TryFindSystemTimeZoneById(zoneId, out result))
            return result;

        if (TryLookupAbbr(zoneId, out result))
            return result;

        throw new TimeZoneNotFoundException($"The time zone ID '{zoneId}' was not found on the local computer.");
#else
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            if (TryLookupAbbr(zoneId, out var result))
                return result;

            throw;
        }
#endif
    }
}