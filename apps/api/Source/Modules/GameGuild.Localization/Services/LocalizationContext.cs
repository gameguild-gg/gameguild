using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace GameGuild.Localization;

/// <summary>
/// Request-scoped implementation of localization context.
/// Reads culture from request headers (Accept-Language) with tenant/user preference support.
/// </summary>
public class LocalizationContext : ILocalizationContext
{
    private static readonly CultureInfo DefaultCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly TimeZoneInfo DefaultTimeZone = TimeZoneInfo.Utc;
    private static readonly HashSet<string> SupportedCultureNames = CultureInfo
        .GetCultures(CultureTypes.SpecificCultures | CultureTypes.NeutralCultures)
        .Select(culture => culture.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    
    private readonly CultureInfo _currentCulture;
    private readonly CultureInfo _currentUiCulture;
    private readonly TimeZoneInfo _currentTimeZone;

    /// <summary>
    /// Creates a new LocalizationContext from the HTTP request.
    /// </summary>
    /// <param name="httpContextAccessor">HTTP context accessor for reading request headers</param>
    /// <param name="userPreferenceProvider">Optional user preference provider</param>
    public LocalizationContext(
        IHttpContextAccessor httpContextAccessor,
        IUserLocalizationPreferenceProvider? userPreferenceProvider = null)
    {
        var httpContext = httpContextAccessor?.HttpContext;
        
        // Priority: 1. User preference, 2. Accept-Language header, 3. Default
        _currentCulture = ResolveCurrentCulture(httpContext, userPreferenceProvider);
        _currentUiCulture = ResolveCurrentUiCulture(httpContext, userPreferenceProvider);
        _currentTimeZone = ResolveTimeZone(httpContext, userPreferenceProvider);
    }

    /// <summary>
    /// Constructor for testing or non-HTTP scenarios.
    /// </summary>
    public LocalizationContext(CultureInfo culture, TimeZoneInfo timeZone)
    {
        _currentCulture = culture ?? DefaultCulture;
        _currentUiCulture = culture ?? DefaultCulture;
        _currentTimeZone = timeZone ?? DefaultTimeZone;
    }

    /// <summary>
    /// Default constructor for backward compatibility - uses defaults.
    /// </summary>
    public LocalizationContext()
    {
        _currentCulture = DefaultCulture;
        _currentUiCulture = DefaultCulture;
        _currentTimeZone = DefaultTimeZone;
    }

    public CultureInfo CurrentCulture => _currentCulture;

    public CultureInfo CurrentUiCulture => _currentUiCulture;

    public TimeZoneInfo CurrentTimeZone => _currentTimeZone;

    public string TimeZoneId => _currentTimeZone.Id;

    public DateTime GetCurrentLocalTime() => TimeZoneInfo.ConvertTimeFromUtc(SystemClock.UtcNow, CurrentTimeZone);

    public DateTime ConvertToLocalTime(DateTime utcTime) => TimeZoneInfo.ConvertTimeFromUtc(utcTime, CurrentTimeZone);

    public DateTime ConvertToUtcTime(DateTime localTime) => TimeZoneInfo.ConvertTimeToUtc(localTime, CurrentTimeZone);

    private static CultureInfo ResolveCurrentCulture(HttpContext? httpContext, IUserLocalizationPreferenceProvider? preferenceProvider)
    {
        // 1. Try user preference
        var userPref = preferenceProvider?.GetPreferredCulture();
        if (userPref != null)
        {
            return userPref;
        }

        // 2. Try Accept-Language header
        if (httpContext != null)
        {
            var acceptLanguage = httpContext.Request.Headers.AcceptLanguage.FirstOrDefault();
            if (!string.IsNullOrEmpty(acceptLanguage))
            {
                try
                {
                    // Parse first language from header (e.g., "en-US,en;q=0.9" -> "en-US")
                    var primaryLanguage = acceptLanguage.Split(',')[0].Split(';')[0].Trim();
                    if (SupportedCultureNames.Contains(primaryLanguage))
                    {
                        return CultureInfo.GetCultureInfo(primaryLanguage);
                    }
                }
                catch (CultureNotFoundException)
                {
                    // Invalid culture, fall through to default
                }
            }
        }

        // 3. Default
        return DefaultCulture;
    }

    private static CultureInfo ResolveCurrentUiCulture(HttpContext? httpContext, IUserLocalizationPreferenceProvider? preferenceProvider)
    {
        // UI culture follows same logic as culture
        return ResolveCurrentCulture(httpContext, preferenceProvider);
    }

    private static TimeZoneInfo ResolveTimeZone(HttpContext? httpContext, IUserLocalizationPreferenceProvider? preferenceProvider)
    {
        // 1. Try user preference
        var userPref = preferenceProvider?.GetPreferredTimeZone();
        if (userPref != null)
        {
            return userPref;
        }

        // 2. Try X-Timezone header (custom header for timezone)
        if (httpContext != null)
        {
            var timezoneHeader = httpContext.Request.Headers["X-Timezone"].FirstOrDefault();
            if (!string.IsNullOrEmpty(timezoneHeader))
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(timezoneHeader);
                }
                catch (TimeZoneNotFoundException)
                {
                    // Invalid timezone, fall through to default
                }
            }
        }

        // 3. Default
        return DefaultTimeZone;
    }
}

/// <summary>
/// Interface for providing user-specific localization preferences.
/// Implement this to load preferences from user profile or tenant settings.
/// </summary>
public interface IUserLocalizationPreferenceProvider
{
    /// <summary>
    /// Gets the user's preferred culture, or null to use request/default.
    /// </summary>
    CultureInfo? GetPreferredCulture();

    /// <summary>
    /// Gets the user's preferred timezone, or null to use request/default.
    /// </summary>
    TimeZoneInfo? GetPreferredTimeZone();
}

