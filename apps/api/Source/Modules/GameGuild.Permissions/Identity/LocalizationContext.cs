using System.Globalization;
using GameGuild.Permissions.Domain.Abstractions;
using Microsoft.AspNetCore.Http;

namespace GameGuild.Permissions.Infrastructure.Identity;

/// <summary>
///     Extracts localization preferences from user claims and tenant settings
///     Priority: User Preferences > Tenant Defaults > System Defaults
/// </summary>
public class LocalizationContext(IHttpContextAccessor httpContextAccessor, IUserContext userContext, ITenantContext tenantContext) : ILocalizationContext
{
    // Default values
    private const string DefaultCulture = "en-US";

    private const string DefaultTimeZone = "UTC";

    private const string DefaultDateFormat = "MM/dd/yyyy";

    private const string DefaultTimeFormat = "12h";

    public string CultureCode
    {
        get
        {
            // Try user claim
            var userCulture = userContext.Claims.TryGetValue("culture", out var cultureValue) ? cultureValue as string : null;

            if (!string.IsNullOrEmpty(userCulture)) return userCulture;

            // Try tenant setting
            var tenantCulture = tenantContext.Settings.TryGetValue("culture", out var tenantCultureValue) ? tenantCultureValue as string : null;

            if (!string.IsNullOrEmpty(tenantCulture)) return tenantCulture;

            // Try Accept-Language header
            var acceptLanguage = httpContextAccessor.HttpContext?.Request.Headers["Accept-Language"].FirstOrDefault();

            if (!string.IsNullOrEmpty(acceptLanguage))
            {
                var primaryLanguage = acceptLanguage.Split(',').FirstOrDefault()?.Split(';').FirstOrDefault()?.Trim();

                if (!string.IsNullOrEmpty(primaryLanguage)) return primaryLanguage;
            }

            return DefaultCulture;
        }
    }

    public string TimeZoneId
    {
        get
        {
            // Try user claim
            var userTimeZone = userContext.Claims.TryGetValue("timezone", out var timezoneValue) ? timezoneValue as string : null;

            if (!string.IsNullOrEmpty(userTimeZone)) return userTimeZone;

            // Try tenant setting
            var tenantTimeZone = tenantContext.Settings.TryGetValue("timezone", out var tenantTimezoneValue) ? tenantTimezoneValue as string : null;

            if (!string.IsNullOrEmpty(tenantTimeZone)) return tenantTimeZone;

            return DefaultTimeZone;
        }
    }

    public string UICultureCode
    {
        get => CultureCode; // Same as culture for now
    }

    public string DateFormat
    {
        get
        {
            var userFormat = userContext.Claims.TryGetValue("date_format", out var formatValue) ? formatValue as string : null;

            if (!string.IsNullOrEmpty(userFormat)) return userFormat;

            var tenantFormat = tenantContext.Settings.TryGetValue("date_format", out var tenantFormatValue) ? tenantFormatValue as string : null;

            if (!string.IsNullOrEmpty(tenantFormat)) return tenantFormat;

            return DefaultDateFormat;
        }
    }

    public string TimeFormat
    {
        get
        {
            var userFormat = userContext.Claims.TryGetValue("time_format", out var formatValue) ? formatValue as string : null;

            if (!string.IsNullOrEmpty(userFormat)) return userFormat;

            var tenantFormat = tenantContext.Settings.TryGetValue("time_format", out var tenantFormatValue) ? tenantFormatValue as string : null;

            if (!string.IsNullOrEmpty(tenantFormat)) return tenantFormat;

            return DefaultTimeFormat;
        }
    }

    public bool UseMetric
    {
        get
        {
            var userMetric = userContext.Claims.TryGetValue("use_metric", out var metricValue) ? metricValue?.ToString() : null;

            if (bool.TryParse(userMetric, out var userUseMetric)) return userUseMetric;

            var tenantMetric = tenantContext.Settings.TryGetValue("use_metric", out var tenantMetricValue) ? tenantMetricValue?.ToString() : null;

            if (bool.TryParse(tenantMetric, out var tenantUseMetric)) return tenantUseMetric;

            // Default to metric for most of the world, except US
            return CultureCode != "en-US";
        }
    }

    public DateTime ToLocalTime(DateTime utcDateTime)
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return utcDateTime; // Fallback to UTC if conversion fails
        }
        catch (InvalidTimeZoneException)
        {
            return utcDateTime; // Fallback to UTC if conversion fails
        }
    }

    public DateTime ToUtcTime(DateTime localDateTime)
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return localDateTime; // Fallback to assuming it's UTC
        }
        catch (InvalidTimeZoneException)
        {
            return localDateTime; // Fallback to assuming it's UTC
        }
    }

    public string FormatDateTime(DateTime dateTime)
    {
        try
        {
            var culture = new CultureInfo(CultureCode);
            var localTime = ToLocalTime(dateTime);

            var timePattern = TimeFormat == "24h" ? "HH:mm" : "hh:mm tt";
            var formatString = $"{DateFormat} {timePattern}";

            return localTime.ToString(formatString, culture);
        }
        catch (CultureNotFoundException)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture); // ISO format fallback
        }
        catch (FormatException)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture); // ISO format fallback
        }
    }
}
