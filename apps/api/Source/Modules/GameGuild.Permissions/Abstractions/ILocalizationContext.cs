namespace GameGuild.Permissions.Domain.Abstractions;

/// <summary>
///     Provides localization context for the current request
///     Includes culture, timezone, and formatting preferences
/// </summary>
public interface ILocalizationContext
{
    /// <summary>
    ///     Gets the current culture code (e.g., "en-US", "fr-FR")
    /// </summary>
    string CultureCode { get; }

    /// <summary>
    ///     Gets the current timezone ID (e.g., "America/New_York", "UTC")
    /// </summary>
    string TimeZoneId { get; }

    /// <summary>
    ///     Gets the current UI culture code for resource lookup
    /// </summary>
    string UICultureCode { get; }

    /// <summary>
    ///     Gets the date format preference (e.g., "MM/DD/YYYY", "DD/MM/YYYY")
    /// </summary>
    string DateFormat { get; }

    /// <summary>
    ///     Gets the time format preference (e.g., "12h", "24h")
    /// </summary>
    string TimeFormat { get; }

    /// <summary>
    ///     Gets whether to use metric or imperial units
    /// </summary>
    bool UseMetric { get; }

    /// <summary>
    ///     Converts a UTC datetime to the user's local timezone
    /// </summary>
    /// <param name="utcDateTime">The UTC datetime to convert</param>
    /// <returns>DateTime in user's timezone</returns>
    DateTime ToLocalTime(DateTime utcDateTime);

    /// <summary>
    ///     Converts a local datetime to UTC
    /// </summary>
    /// <param name="localDateTime">The local datetime to convert</param>
    /// <returns>DateTime in UTC</returns>
    DateTime ToUtcTime(DateTime localDateTime);

    /// <summary>
    ///     Formats a datetime according to user preferences
    /// </summary>
    /// <param name="dateTime">The datetime to format</param>
    /// <returns>Formatted datetime string</returns>
    string FormatDateTime(DateTime dateTime);
}
