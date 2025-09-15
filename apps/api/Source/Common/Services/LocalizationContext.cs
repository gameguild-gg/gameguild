using System.Collections.Concurrent;
using System.Globalization;
using GameGuild.Modules.Tenants;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;


namespace GameGuild.Common.Services;

/// <summary>
/// Implementation of localization context
/// Provides culture, timezone, and regional settings management
/// </summary>
public class LocalizationContext : ILocalizationContext
{
  private readonly IUserContext _userContext;
  private readonly ITenantContext _tenantContext;
  private readonly ITenantSettingsService _tenantSettingsService;
  private readonly IStringLocalizer _localizer;
  private readonly ILogger<LocalizationContext> _logger;

  // Thread-safe current context storage
  private readonly ConcurrentDictionary<string, object> _contextData = new();
  private readonly object _lock = new();

  // Default settings
  private static readonly CultureInfo DefaultCulture = CultureInfo.GetCultureInfo("en-US");
  private static readonly TimeZoneInfo DefaultTimeZone = TimeZoneInfo.Utc;

  // Right-to-left languages
  private static readonly HashSet<string> RtlLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "ar", "he", "fa", "ur", "ps", "sd", "ku", "dv", "arc", "bcc", "bqi", "ckb", "glk", "lrc", "mzn", "pnb", "bal"
    };

  public LocalizationContext(
      IUserContext userContext,
      ITenantContext tenantContext,
      ITenantSettingsService tenantSettingsService,
      IStringLocalizer<LocalizationContext> localizer,
      ILogger<LocalizationContext> logger)
  {
    _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    _tenantSettingsService = tenantSettingsService ?? throw new ArgumentNullException(nameof(tenantSettingsService));
    _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // Initialize context asynchronously
    _ = Task.Run(InitializeContextAsync);
  }

  // === CULTURE PROPERTIES ===

  public CultureInfo CurrentCulture
  {
    get
    {
      if (_contextData.TryGetValue("CurrentCulture", out var culture) && culture is CultureInfo cultureInfo)
        return cultureInfo;

      return DefaultCulture;
    }
  }

  public CultureInfo CurrentUICulture
  {
    get
    {
      if (_contextData.TryGetValue("CurrentUICulture", out var culture) && culture is CultureInfo cultureInfo)
        return cultureInfo;

      return CurrentCulture; // Fallback to current culture
    }
  }

  public string LanguageCode => CurrentCulture.TwoLetterISOLanguageName;

  public string LanguageRegionCode => CurrentCulture.Name;

  public TextDirection TextDirection =>
      RtlLanguages.Contains(LanguageCode) ? TextDirection.RightToLeft : TextDirection.LeftToRight;

  // === TIMEZONE PROPERTIES ===

  public TimeZoneInfo CurrentTimeZone
  {
    get
    {
      if (_contextData.TryGetValue("CurrentTimeZone", out var timeZone) && timeZone is TimeZoneInfo timeZoneInfo)
        return timeZoneInfo;

      return DefaultTimeZone;
    }
  }

  public string TimeZoneId => CurrentTimeZone.Id;

  public TimeSpan UtcOffset => CurrentTimeZone.GetUtcOffset(DateTime.UtcNow);

  public bool IsDaylightSavingTime => CurrentTimeZone.IsDaylightSavingTime(DateTime.Now);

  // === FORMAT PROPERTIES ===

  public string DateFormat
  {
    get
    {
      if (_contextData.TryGetValue("DateFormat", out var format) && format is string dateFormat && !string.IsNullOrEmpty(dateFormat))
        return dateFormat;

      return CurrentCulture.DateTimeFormat.ShortDatePattern;
    }
  }

  public string TimeFormat
  {
    get
    {
      if (_contextData.TryGetValue("TimeFormat", out var format) && format is string timeFormat && !string.IsNullOrEmpty(timeFormat))
        return timeFormat;

      return Use24HourFormat ? "HH:mm" : CurrentCulture.DateTimeFormat.ShortTimePattern;
    }
  }

  public string DateTimeFormat => $"{DateFormat} {TimeFormat}";

  public bool Use24HourFormat
  {
    get
    {
      if (_contextData.TryGetValue("Use24HourFormat", out var use24Hour) && use24Hour is bool use24HourBool)
        return use24HourBool;

      return !CurrentCulture.DateTimeFormat.ShortTimePattern.Contains("tt"); // Check if AM/PM is used
    }
  }

  public NumberFormatInfo NumberFormat => CurrentCulture.NumberFormat;

  public string CurrencySymbol
  {
    get
    {
      if (_contextData.TryGetValue("CurrencySymbol", out var symbol) && symbol is string currencySymbol && !string.IsNullOrEmpty(currencySymbol))
        return currencySymbol;

      return CurrentCulture.NumberFormat.CurrencySymbol;
    }
  }

  public string CurrencyCode
  {
    get
    {
      if (_contextData.TryGetValue("CurrencyCode", out var code) && code is string currencyCode && !string.IsNullOrEmpty(currencyCode))
        return currencyCode;

      // Try to get ISO currency code from region info
      try
      {
        var regionInfo = new RegionInfo(CurrentCulture.Name);
        return regionInfo.ISOCurrencySymbol;
      }
      catch
      {
        return "USD"; // Default fallback
      }
    }
  }

  // === DATE/TIME CONVERSION ===

  public DateTime ToLocalTime(DateTime utcTime)
  {
    if (utcTime.Kind != DateTimeKind.Utc)
    {
      throw new ArgumentException("DateTime must be UTC", nameof(utcTime));
    }

    return TimeZoneInfo.ConvertTimeFromUtc(utcTime, CurrentTimeZone);
  }

  public DateTime ToUtcTime(DateTime localTime)
  {
    return TimeZoneInfo.ConvertTimeToUtc(localTime, CurrentTimeZone);
  }

  public DateTimeOffset ToLocalTime(DateTimeOffset dateTimeOffset)
  {
    return TimeZoneInfo.ConvertTime(dateTimeOffset, CurrentTimeZone);
  }

  public DateTime GetCurrentLocalTime()
  {
    return ToLocalTime(DateTime.UtcNow);
  }

  public DateTimeOffset GetCurrentLocalTimeOffset()
  {
    return ToLocalTime(DateTimeOffset.UtcNow);
  }

  // === FORMATTING HELPERS ===

  public string FormatDateTime(DateTime dateTime, DateTimeFormatType formatType = DateTimeFormatType.DateTime)
  {
    try
    {
      var localDateTime = dateTime.Kind == DateTimeKind.Utc ? ToLocalTime(dateTime) : dateTime;

      return formatType switch
      {
        DateTimeFormatType.Date => localDateTime.ToString(DateFormat, CurrentCulture),
        DateTimeFormatType.Time => localDateTime.ToString(TimeFormat, CurrentCulture),
        DateTimeFormatType.DateTime => localDateTime.ToString(DateTimeFormat, CurrentCulture),
        DateTimeFormatType.ShortDate => localDateTime.ToString("d", CurrentCulture),
        DateTimeFormatType.LongDate => localDateTime.ToString("D", CurrentCulture),
        DateTimeFormatType.ShortTime => localDateTime.ToString("t", CurrentCulture),
        DateTimeFormatType.LongTime => localDateTime.ToString("T", CurrentCulture),
        DateTimeFormatType.Relative => FormatRelativeTime(localDateTime),
        DateTimeFormatType.Iso8601 => dateTime.Kind == DateTimeKind.Utc ?
            dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ") :
            dateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
        _ => localDateTime.ToString(DateTimeFormat, CurrentCulture)
      };
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Error formatting DateTime {DateTime} with format type {FormatType}", dateTime, formatType);
      return dateTime.ToString(CultureInfo.InvariantCulture);
    }
  }

  public string FormatDateTime(DateTimeOffset dateTimeOffset, DateTimeFormatType formatType = DateTimeFormatType.DateTime)
  {
    try
    {
      var localDateTime = ToLocalTime(dateTimeOffset);

      return formatType switch
      {
        DateTimeFormatType.Date => localDateTime.ToString(DateFormat, CurrentCulture),
        DateTimeFormatType.Time => localDateTime.ToString(TimeFormat, CurrentCulture),
        DateTimeFormatType.DateTime => localDateTime.ToString(DateTimeFormat, CurrentCulture),
        DateTimeFormatType.ShortDate => localDateTime.ToString("d", CurrentCulture),
        DateTimeFormatType.LongDate => localDateTime.ToString("D", CurrentCulture),
        DateTimeFormatType.ShortTime => localDateTime.ToString("t", CurrentCulture),
        DateTimeFormatType.LongTime => localDateTime.ToString("T", CurrentCulture),
        DateTimeFormatType.Relative => FormatRelativeTime(localDateTime.DateTime),
        DateTimeFormatType.Iso8601 => dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:sszzz"),
        _ => localDateTime.ToString(DateTimeFormat, CurrentCulture)
      };
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Error formatting DateTimeOffset {DateTimeOffset} with format type {FormatType}", dateTimeOffset, formatType);
      return dateTimeOffset.ToString(CultureInfo.InvariantCulture);
    }
  }

  public string FormatNumber(decimal number, NumberFormatType formatType = NumberFormatType.Number)
  {
    try
    {
      return formatType switch
      {
        NumberFormatType.Number => number.ToString("N", CurrentCulture),
        NumberFormatType.Decimal => number.ToString("F", CurrentCulture),
        NumberFormatType.Percent => number.ToString("P", CurrentCulture),
        NumberFormatType.Scientific => number.ToString("E", CurrentCulture),
        NumberFormatType.FixedPoint => number.ToString("F", CurrentCulture),
        _ => number.ToString(CurrentCulture)
      };
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Error formatting number {Number} with format type {FormatType}", number, formatType);
      return number.ToString(CultureInfo.InvariantCulture);
    }
  }

  public string FormatCurrency(decimal amount)
  {
    try
    {
      return amount.ToString("C", CurrentCulture);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Error formatting currency {Amount}", amount);
      return amount.ToString("C", CultureInfo.InvariantCulture);
    }
  }

  // === LOCALIZATION HELPERS ===

  public string GetLocalizedString(string key, string? defaultValue = null, params object[] args)
  {
    try
    {
      var localizedString = _localizer[key];

      if (localizedString.ResourceNotFound && !string.IsNullOrEmpty(defaultValue))
      {
        return string.Format(defaultValue, args);
      }

      return string.Format(localizedString.Value, args);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Error getting localized string for key {Key}", key);
      return defaultValue ?? key;
    }
  }

  public bool HasLocalizationKey(string key)
  {
    try
    {
      var localizedString = _localizer[key];
      return !localizedString.ResourceNotFound;
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Error checking localization key {Key}", key);
      return false;
    }
  }

  public IEnumerable<string> GetAvailableLanguages()
  {
    // This could be configured from tenant settings or application configuration
    // For now, return a default set of supported languages
    return new[] { "en", "en-US", "pt", "pt-BR", "es", "es-ES", "fr", "fr-FR", "de", "de-DE" };
  }

  public IEnumerable<TimeZoneInfo> GetAvailableTimeZones()
  {
    return TimeZoneInfo.GetSystemTimeZones();
  }

  // === CONTEXT MANAGEMENT ===

  public void SetCulture(CultureInfo culture, CultureInfo? uiCulture = null)
  {
    lock (_lock)
    {
      _contextData["CurrentCulture"] = culture ?? throw new ArgumentNullException(nameof(culture));
      _contextData["CurrentUICulture"] = uiCulture ?? culture;

      _logger.LogDebug("Culture set to {Culture}, UI Culture set to {UICulture}",
          culture.Name, (uiCulture ?? culture).Name);
    }
  }

  public void SetCulture(string languageCode)
  {
    try
    {
      var culture = CultureInfo.GetCultureInfo(languageCode);
      SetCulture(culture);
    }
    catch (CultureNotFoundException ex)
    {
      _logger.LogWarning(ex, "Invalid language code: {LanguageCode}. Using default culture.", languageCode);
      SetCulture(DefaultCulture);
    }
  }

  public void SetTimeZone(string timeZoneId)
  {
    try
    {
      var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
      SetTimeZone(timeZone);
    }
    catch (TimeZoneNotFoundException ex)
    {
      _logger.LogWarning(ex, "Invalid timezone ID: {TimeZoneId}. Using default timezone.", timeZoneId);
      SetTimeZone(DefaultTimeZone);
    }
  }

  public void SetTimeZone(TimeZoneInfo timeZone)
  {
    lock (_lock)
    {
      _contextData["CurrentTimeZone"] = timeZone ?? throw new ArgumentNullException(nameof(timeZone));

      _logger.LogDebug("Timezone set to {TimeZone}", timeZone.Id);
    }
  }

  public void ResetToDefaults()
  {
    lock (_lock)
    {
      _contextData.Clear();
      _logger.LogDebug("Localization context reset to defaults");
    }

    // Re-initialize from settings
    _ = Task.Run(InitializeContextAsync);
  }

  // === PRIVATE HELPERS ===

  private async Task InitializeContextAsync()
  {
    try
    {
      // Get tenant and user contexts
      var tenantId = _tenantContext.TenantId;
      var userId = _userContext.UserId;

      if (tenantId.HasValue)
      {
        // Load tenant settings
        var tenantSettings = await _tenantSettingsService.GetTenantSettingsAsync(tenantId.Value);

        if (tenantSettings != null)
        {
          // Apply tenant localization settings
          ApplyTenantLocalizationSettings(tenantSettings);
        }
      }

      // Apply user preferences if available
      // This could be extended to load user-specific localization preferences
      // For now, we'll use tenant settings or defaults

      _logger.LogDebug("Localization context initialized for Tenant {TenantId}, User {UserId}", tenantId, userId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error initializing localization context. Using defaults.");

      // Set defaults on error
      lock (_lock)
      {
        _contextData["CurrentCulture"] = DefaultCulture;
        _contextData["CurrentUICulture"] = DefaultCulture;
        _contextData["CurrentTimeZone"] = DefaultTimeZone;
      }
    }
  }

  private void ApplyTenantLocalizationSettings(dynamic tenantSettings)
  {
    try
    {
      lock (_lock)
      {
        // Apply localization settings from tenant settings
        if (tenantSettings.LocalizationSettings != null)
        {
          var localizationSettings = tenantSettings.LocalizationSettings;

          // Default language and culture
          if (!string.IsNullOrEmpty((string?)localizationSettings.DefaultLanguage))
          {
            try
            {
              var culture = CultureInfo.GetCultureInfo((string)localizationSettings.DefaultLanguage);
              _contextData["CurrentCulture"] = culture;
              _contextData["CurrentUICulture"] = culture;
            }
            catch (CultureNotFoundException ex)
            {
              _logger.LogWarning(ex, "Invalid default language in tenant settings: {Language}",
                  (string?)localizationSettings.DefaultLanguage);
            }
          }

          // Timezone
          if (!string.IsNullOrEmpty((string?)localizationSettings.DefaultTimezone))
          {
            try
            {
              var timeZone = TimeZoneInfo.FindSystemTimeZoneById((string)localizationSettings.DefaultTimezone);
              _contextData["CurrentTimeZone"] = timeZone;
            }
            catch (TimeZoneNotFoundException ex)
            {
              _logger.LogWarning(ex, "Invalid default timezone in tenant settings: {TimeZone}",
                  (string?)localizationSettings.DefaultTimezone);
            }
          }

          // Date formats
          if (!string.IsNullOrEmpty((string?)localizationSettings.DateFormat))
          {
            _contextData["DateFormat"] = (string)localizationSettings.DateFormat;
          }

          if (!string.IsNullOrEmpty((string?)localizationSettings.TimeFormat))
          {
            _contextData["TimeFormat"] = (string)localizationSettings.TimeFormat;
          }

          // 24-hour format
          if (localizationSettings.Use24HourFormat != null)
          {
            _contextData["Use24HourFormat"] = (bool)localizationSettings.Use24HourFormat;
          }

          // Currency
          if (!string.IsNullOrEmpty((string?)localizationSettings.DefaultCurrency))
          {
            _contextData["CurrencyCode"] = (string)localizationSettings.DefaultCurrency;
          }
        }
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error applying tenant localization settings");
    }
  }

  private string FormatRelativeTime(DateTime dateTime)
  {
    var now = GetCurrentLocalTime();
    var timeSpan = now - dateTime;

    if (timeSpan.TotalSeconds < 60)
    {
      return GetLocalizedString("time.justNow", "just now");
    }
    else if (timeSpan.TotalMinutes < 60)
    {
      var minutes = (int)timeSpan.TotalMinutes;
      return GetLocalizedString("time.minutesAgo", "{0} minute(s) ago", minutes);
    }
    else if (timeSpan.TotalHours < 24)
    {
      var hours = (int)timeSpan.TotalHours;
      return GetLocalizedString("time.hoursAgo", "{0} hour(s) ago", hours);
    }
    else if (timeSpan.TotalDays < 30)
    {
      var days = (int)timeSpan.TotalDays;
      return GetLocalizedString("time.daysAgo", "{0} day(s) ago", days);
    }
    else
    {
      return FormatDateTime(dateTime, DateTimeFormatType.Date);
    }
  }
}
