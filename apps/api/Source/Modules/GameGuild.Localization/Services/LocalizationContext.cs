using System.Globalization;
using GameGuild.Core.Domain.Identity;
using GameGuild.Modules.Tenants;

namespace GameGuild.Modules.Localization;

/// <summary>
/// Simple implementation of localization context
/// </summary>
#pragma warning disable CS9113 // Parameter is unread
public class LocalizationContext(ITenantContext tenantContext, ILogger<LocalizationContext> logger) : ILocalizationContext
#pragma warning restore CS9113
{
  private static readonly CultureInfo DefaultCulture = CultureInfo.GetCultureInfo("en-US");

  private static readonly TimeZoneInfo DefaultTimeZone = TimeZoneInfo.Utc;

  public CultureInfo CurrentCulture => DefaultCulture;

  public CultureInfo CurrentUiCulture => DefaultCulture;

  public TimeZoneInfo CurrentTimeZone => DefaultTimeZone;

  public string TimeZoneId => DefaultTimeZone.Id;

  public DateTime GetCurrentLocalTime() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CurrentTimeZone);

  public DateTime ConvertToLocalTime(DateTime utcTime) => TimeZoneInfo.ConvertTimeFromUtc(utcTime, CurrentTimeZone);

  public DateTime ConvertToUtcTime(DateTime localTime) => TimeZoneInfo.ConvertTimeToUtc(localTime, CurrentTimeZone);
}
