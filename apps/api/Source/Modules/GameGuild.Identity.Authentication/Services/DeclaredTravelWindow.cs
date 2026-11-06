using System.Globalization;
using System.Text.Json;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     A user-declared travel window (issue #1115): an optional date range the account
///     owner declares through their settings so legitimate travel does not trip the
///     GeoVelocity (impossible-travel) anomaly detector on every sign-in.
///     Windows are stored in the user's general preferences as
///     <c>{"travelWindows":[{"start":"2026-11-01","end":"2026-11-15"}]}</c> and are only
///     consulted after a suspicious geo transition has already been detected, so the
///     happy path pays no extra cost. Parsing and coverage checks are deliberately
///     fail-closed: any malformed input means "no declared travel" and the alert fires.
/// </summary>
public static class DeclaredTravelWindow
{
    /// <summary>Preferences key holding the declared window array.</summary>
    public static string PreferencesKey => "travelWindows";

    /// <summary>
    ///     Maximum length of a single declared window. A longer range is ignored rather
    ///     than suppressing alerts for months from one declaration.
    /// </summary>
    public static readonly TimeSpan MaxWindowLength = TimeSpan.FromDays(31);

    /// <summary>
    ///     Returns true when at least one declared window covers <paramref name="instant" />.
    ///     Values arrive from the JSON round-tripped preferences dictionary, so both
    ///     <see cref="JsonElement" /> payloads and unexpected shapes must be tolerated.
    /// </summary>
    public static bool Covers(Dictionary<string, object?> generalPreferences, DateTime instant)
    {
        ArgumentNullException.ThrowIfNull(generalPreferences);

        if (!generalPreferences.TryGetValue(PreferencesKey, out var raw) || raw is null)
        {
            return false;
        }

        // The general preferences dictionary comes from deserializing a JSON blob, so the
        // value is a boxed JsonElement; anything else (hand-built dictionary in tests)
        // is not a window declaration we can trust.
        if (raw is not JsonElement { ValueKind: JsonValueKind.Array } windows)
        {
            return false;
        }

        foreach (var window in windows.EnumerateArray())
        {
            if (TryParseWindow(window, out var start, out var end) &&
                instant >= start && instant <= end)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseWindow(JsonElement window, out DateTime start, out DateTime end)
    {
        start = default;
        end = default;

        if (window is not { ValueKind: JsonValueKind.Object })
        {
            return false;
        }

        if (!TryReadUtcTimestamp(window, "start", out start) ||
            !TryReadUtcTimestamp(window, "end", out end))
        {
            return false;
        }

        if (end < start || end - start > MaxWindowLength)
        {
            // Nonsensical or over-long declarations never suppress an alert.
            return false;
        }

        return true;
    }

    private static bool TryReadUtcTimestamp(JsonElement window, string property, out DateTime value)
    {
        value = default;

        if (!window.TryGetProperty(property, out var element))
        {
            return false;
        }

        if (element.ValueKind != JsonValueKind.String ||
            !DateTime.TryParse(
                element.GetString(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out value))
        {
            return false;
        }

        return true;
    }
}
