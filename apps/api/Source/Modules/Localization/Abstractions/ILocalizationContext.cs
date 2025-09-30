using System.Globalization;

namespace GameGuild.Core.Domain.Identity;

/// <summary> Context interface for localization within a request scope </summary>
public interface ILocalizationContext
{
    /// <summary> Gets the current culture </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary> Gets the current UI culture </summary>
    CultureInfo CurrentUiCulture { get; }

    /// <summary> Gets the current time zone </summary>
    TimeZoneInfo CurrentTimeZone { get; }

    /// <summary> Gets the current time zone ID </summary>
    string TimeZoneId { get; }

    /// <summary> Gets the current local time in the user's time zone </summary>
    /// <returns> Current local time </returns>
    DateTime GetCurrentLocalTime();

    /// <summary> Converts UTC time to local time using the current time zone </summary>
    /// <param name="utcTime"> UTC time to convert </param>
    /// <param name="localTime"> Local time </param>
    /// <returns> Local time </returns>
    DateTime ConvertToLocalTime(DateTime utcTime);

    /// <summary> Converts local time to UTC using the current time zone </summary>
    /// <param name="localTime"> Local time to convert </param>
    /// <returns> UTC time </returns>
    DateTime ConvertToUtcTime(DateTime localTime);
}
