using System.Text.Json;

namespace GameGuild.Identity.Users;

/// <summary>
///     Validation for the currency preference inside the user localization preferences
///     (issue #412 spec E): the account-level preferred currency must be a known ISO 4217
///     code. The stored value is always normalized upper-case so pricing surfaces can use
///     it directly; unknown codes fail closed instead of silently persisting junk.
/// </summary>
public static class UserCurrencyPreference
{
    /// <summary>Localization-preferences key holding the preferred currency code.</summary>
    public const string PreferenceKey = "Currency";

    /// <summary>
    ///     Normalizes (and validates) the currency entries of a localization-preferences
    ///     payload. Non-currency keys pass through untouched; a currency value that is not a
    ///     three-letter ISO 4217 code in the platform catalog throws.
    /// </summary>
    /// <param name="preferences">Preference payload about to be persisted.</param>
    /// <exception cref="BusinessRuleViolationException">
    ///     Thrown when a currency value is not a known ISO 4217 code.
    /// </exception>
    public static Dictionary<string, object?> NormalizeAndValidate(Dictionary<string, object?> preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        foreach (var (key, value) in preferences)
        {
            if (!string.Equals(key, PreferenceKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var candidate = value switch
            {
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                string text => text,
                _ => null,
            };

            var normalized = (candidate ?? string.Empty).Trim().ToUpperInvariant();
            if (normalized.Length != 3 || !Iso4217CurrencyCatalog.Exists(normalized))
            {
                throw new BusinessRuleViolationException(
                    "InvalidPreferredCurrency",
                    $"'{candidate}' is not a known ISO 4217 currency; the preferred currency must be a three-letter code from the platform catalog.",
                    new { value = candidate });
            }

            preferences[key] = normalized;
        }

        return preferences;
    }
}
