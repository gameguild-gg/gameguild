using System.Globalization;

namespace GameGuild;

/// <summary>
/// Interface for accessing current localization context
/// Provides culture, timezone, and regional settings for the current request
/// </summary>
public interface ILocalizationContext
{
    // === CULTURE SETTINGS ===

    /// <summary>
    /// Current culture for formatting and localization
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Current UI culture for user interface translations
    /// </summary>
    CultureInfo CurrentUICulture { get; }

    /// <summary>
    /// Language code (e.g., "en", "pt", "es")
    /// </summary>
    string LanguageCode { get; }

    /// <summary>
    /// Full language and region code (e.g., "en-US", "pt-BR")
    /// </summary>
    string LanguageRegionCode { get; }

    /// <summary>
    /// Text direction (LTR/RTL)
    /// </summary>
    TextDirection TextDirection { get; }

    // === TIMEZONE SETTINGS ===

    /// <summary>
    /// Current timezone
    /// </summary>
    TimeZoneInfo CurrentTimeZone { get; }

    /// <summary>
    /// Timezone identifier (e.g., "UTC", "America/New_York")
    /// </summary>
    string TimeZoneId { get; }

    /// <summary>
    /// UTC offset for current timezone
    /// </summary>
    TimeSpan UtcOffset { get; }

    /// <summary>
    /// Whether current timezone observes daylight saving time
    /// </summary>
    bool IsDaylightSavingTime { get; }

    // === FORMATTING SETTINGS ===

    /// <summary>
    /// Date format pattern
    /// </summary>
    string DateFormat { get; }

    /// <summary>
    /// Time format pattern
    /// </summary>
    string TimeFormat { get; }

    /// <summary>
    /// DateTime format pattern
    /// </summary>
    string DateTimeFormat { get; }

    /// <summary>
    /// Whether to use 24-hour time format
    /// </summary>
    bool Use24HourFormat { get; }

    /// <summary>
    /// Number format info
    /// </summary>
    NumberFormatInfo NumberFormat { get; }

    /// <summary>
    /// Currency symbol
    /// </summary>
    string CurrencySymbol { get; }

    /// <summary>
    /// Currency code (ISO 4217)
    /// </summary>
    string CurrencyCode { get; }

    // === DATE/TIME CONVERSION ===

    /// <summary>
    /// Convert UTC time to current timezone
    /// </summary>
    /// <param name="utcTime">UTC DateTime</param>
    /// <returns>Local time in current timezone</returns>
    DateTime ToLocalTime(DateTime utcTime);

    /// <summary>
    /// Convert local time to UTC
    /// </summary>
    /// <param name="localTime">Local DateTime</param>
    /// <returns>UTC time</returns>
    DateTime ToUtcTime(DateTime localTime);

    /// <summary>
    /// Convert DateTimeOffset to current timezone
    /// </summary>
    /// <param name="dateTimeOffset">DateTimeOffset</param>
    /// <returns>DateTimeOffset in current timezone</returns>
    DateTimeOffset ToLocalTime(DateTimeOffset dateTimeOffset);

    /// <summary>
    /// Get current date/time in local timezone
    /// </summary>
    /// <returns>Current local time</returns>
    DateTime GetCurrentLocalTime();

    /// <summary>
    /// Get current date/time as DateTimeOffset in local timezone
    /// </summary>
    /// <returns>Current local DateTimeOffset</returns>
    DateTimeOffset GetCurrentLocalTimeOffset();

    // === FORMATTING HELPERS ===

    /// <summary>
    /// Format DateTime according to current culture and format settings
    /// </summary>
    /// <param name="dateTime">DateTime to format</param>
    /// <param name="formatType">Format type</param>
    /// <returns>Formatted date/time string</returns>
    string FormatDateTime(DateTime dateTime, DateTimeFormatType formatType = DateTimeFormatType.DateTime);

    /// <summary>
    /// Format DateTimeOffset according to current culture and format settings
    /// </summary>
    /// <param name="dateTimeOffset">DateTimeOffset to format</param>
    /// <param name="formatType">Format type</param>
    /// <returns>Formatted date/time string</returns>
    string FormatDateTime(DateTimeOffset dateTimeOffset, DateTimeFormatType formatType = DateTimeFormatType.DateTime);

    /// <summary>
    /// Format number according to current culture
    /// </summary>
    /// <param name="number">Number to format</param>
    /// <param name="formatType">Number format type</param>
    /// <returns>Formatted number string</returns>
    string FormatNumber(decimal number, NumberFormatType formatType = NumberFormatType.Number);

    /// <summary>
    /// Format currency according to current culture and currency settings
    /// </summary>
    /// <param name="amount">Currency amount</param>
    /// <returns>Formatted currency string</returns>
    string FormatCurrency(decimal amount);

    // === LOCALIZATION HELPERS ===

    /// <summary>
    /// Get localized string for key
    /// </summary>
    /// <param name="key">Localization key</param>
    /// <param name="defaultValue">Default value if key not found</param>
    /// <param name="args">Format arguments</param>
    /// <returns>Localized string</returns>
    string GetLocalizedString(string key, string? defaultValue = null, params object[] args);

    /// <summary>
    /// Check if localization key exists
    /// </summary>
    /// <param name="key">Localization key</param>
    /// <returns>True if key exists</returns>
    bool HasLocalizationKey(string key);

    /// <summary>
    /// Get all available languages
    /// </summary>
    /// <returns>Available language codes</returns>
    IEnumerable<string> GetAvailableLanguages();

    /// <summary>
    /// Get all available timezones
    /// </summary>
    /// <returns>Available timezone information</returns>
    IEnumerable<TimeZoneInfo> GetAvailableTimeZones();

    // === CONTEXT MANAGEMENT ===

    /// <summary>
    /// Set culture context
    /// </summary>
    /// <param name="culture">Culture to set</param>
    /// <param name="uiCulture">UI culture to set (null to use same as culture)</param>
    void SetCulture(CultureInfo culture, CultureInfo? uiCulture = null);

    /// <summary>
    /// Set culture context by language code
    /// </summary>
    /// <param name="languageCode">Language code (e.g., "en-US", "pt-BR")</param>
    void SetCulture(string languageCode);

    /// <summary>
    /// Set timezone context
    /// </summary>
    /// <param name="timeZoneId">Timezone identifier</param>
    void SetTimeZone(string timeZoneId);

    /// <summary>
    /// Set timezone context
    /// </summary>
    /// <param name="timeZone">TimeZone info</param>
    void SetTimeZone(TimeZoneInfo timeZone);

    /// <summary>
    /// Reset to default culture and timezone
    /// </summary>
    void ResetToDefaults();
}

/// <summary>
/// Text direction enumeration
/// </summary>
public enum TextDirection
{
    /// <summary>Left-to-right</summary>
    LeftToRight,
    /// <summary>Right-to-left</summary>
    RightToLeft
}

/// <summary>
/// Date/time format type enumeration
/// </summary>
public enum DateTimeFormatType
{
    /// <summary>Date only</summary>
    Date,
    /// <summary>Time only</summary>
    Time,
    /// <summary>Date and time</summary>
    DateTime,
    /// <summary>Short date format</summary>
    ShortDate,
    /// <summary>Long date format</summary>
    LongDate,
    /// <summary>Short time format</summary>
    ShortTime,
    /// <summary>Long time format</summary>
    LongTime,
    /// <summary>Relative time (e.g., "2 hours ago")</summary>
    Relative,
    /// <summary>ISO 8601 format</summary>
    Iso8601
}

/// <summary>
/// Number format type enumeration
/// </summary>
public enum NumberFormatType
{
    /// <summary>Standard number</summary>
    Number,
    /// <summary>Decimal number</summary>
    Decimal,
    /// <summary>Percentage</summary>
    Percent,
    /// <summary>Scientific notation</summary>
    Scientific,
    /// <summary>Fixed point</summary>
    FixedPoint
}
