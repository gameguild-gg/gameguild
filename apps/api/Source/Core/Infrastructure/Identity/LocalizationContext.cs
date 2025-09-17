using System.Collections.Concurrent;
using System.Globalization;
using GameGuild.Core.Domain.Identity;


namespace GameGuild.Core.Infrastructure.Identity;

/// <summary>
/// Implementation of localization context
/// Provides culture, timezone, and regional settings management
/// </summary>
public class LocalizationContext : ILocalizationContext {
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<LocalizationContext> _logger;

    // Thread-safe current context storage
    private readonly ConcurrentDictionary<string, object> _contextData = new();
    private readonly object _lock = new();

    // Default settings
    private static readonly CultureInfo DefaultCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly TimeZoneInfo DefaultTimeZone = TimeZoneInfo.Utc;

    public LocalizationContext(
      IUserContext userContext,
      ITenantContext tenantContext,
      ILogger<LocalizationContext> logger) {
        _userContext = userContext;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    // === PROPERTIES ===

    public CultureInfo CurrentCulture {
        get {
            // Try to get culture from user context, then tenant, then default
            if (_userContext.Claims.TryGetValue("culture", out var userCulture) &&
                !string.IsNullOrEmpty(userCulture?.ToString())) {
                try {
                    return CultureInfo.GetCultureInfo(userCulture.ToString()!);
                }
                catch (CultureNotFoundException) {
                    _logger.LogWarning("Invalid culture '{Culture}' found in user context", userCulture);
                }
            }

            if (_tenantContext.Settings.TryGetValue("DefaultCulture", out var tenantCulture) &&
                !string.IsNullOrEmpty(tenantCulture?.ToString())) {
                try {
                    return CultureInfo.GetCultureInfo(tenantCulture.ToString()!);
                }
                catch (CultureNotFoundException) {
                    _logger.LogWarning("Invalid culture '{Culture}' found in tenant context", tenantCulture);
                }
            }

            return DefaultCulture;
        }
    }

    public CultureInfo CurrentUICulture => CurrentCulture; // Simplified - use same as CurrentCulture

    public TimeZoneInfo CurrentTimeZone {
        get {
            // Try to get timezone from user context, then tenant, then default
            if (_userContext.Claims.TryGetValue("timezone", out var userTimezone) &&
                !string.IsNullOrEmpty(userTimezone?.ToString())) {
                try {
                    return TimeZoneInfo.FindSystemTimeZoneById(userTimezone.ToString()!);
                }
                catch (TimeZoneNotFoundException) {
                    _logger.LogWarning("Invalid timezone '{TimeZone}' found in user context", userTimezone);
                }
            }

            if (_tenantContext.Settings.TryGetValue("DefaultTimeZone", out var tenantTimezone) &&
                !string.IsNullOrEmpty(tenantTimezone?.ToString())) {
                try {
                    return TimeZoneInfo.FindSystemTimeZoneById(tenantTimezone.ToString()!);
                }
                catch (TimeZoneNotFoundException) {
                    _logger.LogWarning("Invalid timezone '{TimeZone}' found in tenant context", tenantTimezone);
                }
            }

            return DefaultTimeZone;
        }
    }

    public string TimeZoneId => CurrentTimeZone.Id;

    // === PUBLIC METHODS ===

    public DateTime GetCurrentLocalTime() {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CurrentTimeZone);
    }

    public DateTime ConvertToLocalTime(DateTime utcTime) {
        if (utcTime.Kind != DateTimeKind.Utc) {
            _logger.LogWarning("Converting non-UTC time {Time} to local time, this may produce incorrect results", utcTime);
        }

        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, CurrentTimeZone);
    }

    public DateTime ConvertToUtcTime(DateTime localTime) {
        if (localTime.Kind == DateTimeKind.Utc) {
            return localTime; // Already UTC
        }

        return TimeZoneInfo.ConvertTimeToUtc(localTime, CurrentTimeZone);
    }
}
