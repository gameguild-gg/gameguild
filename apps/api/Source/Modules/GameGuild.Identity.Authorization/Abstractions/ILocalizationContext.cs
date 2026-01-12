namespace GameGuild.Identity.Authorization;

/// <summary>
///     Abstraction for accessing localization information from the request context
/// </summary>
public interface ILocalizationContext
{
    /// <summary>
    ///     Gets the current culture code (e.g., "en-US", "pt-BR")
    /// </summary>
    string? CultureCode { get; }

    /// <summary>
    ///     Gets the current UI culture code
    /// </summary>
    string? UICultureCode { get; }

    /// <summary>
    ///     Gets the current timezone
    /// </summary>
    string? TimeZone { get; }

    /// <summary>
    ///     Gets the preferred date format
    /// </summary>
    string? DateFormat { get; }

    /// <summary>
    ///     Gets the preferred number format
    /// </summary>
    string? NumberFormat { get; }
}
