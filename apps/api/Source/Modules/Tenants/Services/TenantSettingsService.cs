using System.Text.Json;
using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Tenants;

/// <summary> Implementation of tenant settings service Provides complete tenant settings management with caching and fallback support </summary>
public class TenantSettingsService(ApplicationDbContext context, ILogger<TenantSettingsService> logger) : ITenantSettingsService {
  // === BASIC CRUD OPERATIONS ===

  public async Task<TenantSettings> GetTenantSettingsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default) {
    var settings = await context.TenantSettings.Include(s => s.Tenant).FirstOrDefaultAsync(s => s.TenantId == tenantId && s.DeletedAt == null, cancellationToken);

    if (settings == null) {
      logger.LogDebug("No settings found for tenant {TenantId}, creating defaults", tenantId);

      // Create default settings
      settings = TenantSettings.CreateDefault(tenantId);

      // If this is for a specific tenant, verify tenant exists
      if (tenantId.HasValue) {
        var tenant = await context.Tenants.FindAsync([tenantId.Value], cancellationToken);

        if (tenant == null) { throw new ArgumentException($"Tenant with ID {tenantId} not found", nameof(tenantId)); }

        settings.Tenant = tenant;
      }

      context.TenantSettings.Add(settings);
      await context.SaveChangesAsync(cancellationToken);

      logger.LogInformation("Created default settings for tenant {TenantId}", tenantId);
    }

    return settings;
  }

  public async Task<TenantSettings> SaveTenantSettingsAsync(Guid? tenantId, TenantSettings settings, CancellationToken cancellationToken = default) {
    ArgumentNullException.ThrowIfNull(settings);

    var existing = await context.TenantSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.DeletedAt == null, cancellationToken);

    if (existing == null) {
      // Create new settings
      settings.TenantId = tenantId;

      if (tenantId.HasValue) {
        var tenant = await context.Tenants.FindAsync([tenantId.Value], cancellationToken);

        if (tenant == null) { throw new ArgumentException($"Tenant with ID {tenantId} not found", nameof(tenantId)); }

        settings.Tenant = tenant;
      }

      context.TenantSettings.Add(settings);
    }
    else {
      // Update existing settings
      CopySettingsProperties(settings, existing);
      existing.Touch();
    }

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Saved settings for tenant {TenantId}", tenantId);

    return existing ?? settings;
  }

  public async Task<TenantSettings> UpdateTenantSettingsAsync(Guid? tenantId, Dictionary<string, object> updates, CancellationToken cancellationToken = default) {
    ArgumentNullException.ThrowIfNull(updates);

    var settings = await GetTenantSettingsAsync(tenantId, cancellationToken);

    // Use reflection to update properties
    var settingsType = typeof(TenantSettings);

    foreach (var update in updates) {
      var property = settingsType.GetProperty(update.Key);

      if (property != null && property.CanWrite) {
        try {
          var convertedValue = Convert.ChangeType(update.Value, property.PropertyType);
          property.SetValue(settings, convertedValue);
          logger.LogDebug("Updated property {PropertyName} to {Value} for tenant {TenantId}", update.Key, update.Value, tenantId);
        }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to update property {PropertyName} for tenant {TenantId}", update.Key, tenantId); }
      }
    }

    settings.Touch();
    await context.SaveChangesAsync(cancellationToken);

    return settings;
  }

  public async Task<TenantSettings> ResetTenantSettingsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default) {
    var existing = await context.TenantSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.DeletedAt == null, cancellationToken);

    var defaultSettings = TenantSettings.CreateDefault(tenantId);

    if (existing == null) {
      // Create new default settings
      if (tenantId.HasValue) {
        var tenant = await context.Tenants.FindAsync([tenantId.Value], cancellationToken);

        if (tenant == null) { throw new ArgumentException($"Tenant with ID {tenantId} not found", nameof(tenantId)); }

        defaultSettings.Tenant = tenant;
      }

      context.TenantSettings.Add(defaultSettings);
      await context.SaveChangesAsync(cancellationToken);

      return defaultSettings;
    }

    // Reset existing settings to defaults but preserve ID and audit fields
    CopySettingsProperties(defaultSettings, existing);
    existing.Touch();
    await context.SaveChangesAsync(cancellationToken);

    return existing;
  }

  public async Task DeleteTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default) {
    var settings = await context.TenantSettings.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.DeletedAt == null, cancellationToken);

    if (settings != null) {
      settings.SoftDelete();
      _ = await context.SaveChangesAsync(cancellationToken);
      logger.LogInformation("Deleted settings for tenant {TenantId}", tenantId);
    }
  }

  // === FEATURE FLAGS ===

  public async Task<bool> GetFeatureFlagAsync(Guid? tenantId, string key, bool defaultValue = false, CancellationToken cancellationToken = default) {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);

    var settings = await GetTenantSettingsAsync(tenantId, cancellationToken);

    return settings.GetFeatureFlag(key, defaultValue);
  }

  public async Task SetFeatureFlagAsync(Guid? tenantId, string key, bool value, CancellationToken cancellationToken = default) {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);

    var settings = await GetTenantSettingsAsync(tenantId, cancellationToken);
    settings.SetFeatureFlag(key, value);

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Set feature flag {FlagKey} to {Value} for tenant {TenantId}", key, value, tenantId);
  }

  public async Task<Dictionary<string, bool>> GetAllFeatureFlagsAsync(Guid? tenantId, CancellationToken cancellationToken = default) {
    var settings = await GetTenantSettingsAsync(tenantId, cancellationToken);

    if (string.IsNullOrEmpty(settings.FeatureFlags)) { return new Dictionary<string, bool>(); }

    try {
      var flags = JsonSerializer.Deserialize<Dictionary<string, object>>(settings.FeatureFlags) ?? new Dictionary<string, object>();

      return flags.Where(kvp => kvp.Value is bool).ToDictionary(kvp => kvp.Key, kvp => (bool)kvp.Value);
    }
    catch (Exception ex) {
      logger.LogWarning(ex, "Failed to deserialize feature flags for tenant {TenantId}", tenantId);

      return new Dictionary<string, bool>();
    }
  }

  // === MODULE SETTINGS ===

  public async Task<T?> GetModuleSettingAsync<T>(Guid? tenantId, string module, string key, T? defaultValue = default, CancellationToken cancellationToken = default) {
    ArgumentException.ThrowIfNullOrWhiteSpace(module);
    ArgumentException.ThrowIfNullOrWhiteSpace(key);

    var settings = await GetTenantSettingsAsync(tenantId, cancellationToken);

    return settings.GetModuleSetting(module, key, defaultValue);
  }

  public async Task SetModuleSettingAsync<T>(Guid? tenantId, string module, string key, T value, CancellationToken cancellationToken = default) {
    ArgumentException.ThrowIfNullOrWhiteSpace(module);
    ArgumentException.ThrowIfNullOrWhiteSpace(key);

    var settings = await GetTenantSettingsAsync(tenantId, cancellationToken);
    settings.SetModuleSetting(module, key, value);

    await context.SaveChangesAsync(cancellationToken);

    logger.LogInformation("Set module setting {Module}.{Key} to {Value} for tenant {TenantId}", module, key, value, tenantId);
  }

  public async Task<Dictionary<string, object>> GetModuleSettingsAsync(Guid? tenantId, string module, CancellationToken cancellationToken = default) {
    ArgumentException.ThrowIfNullOrWhiteSpace(module);

    var settings = await GetTenantSettingsAsync(tenantId, cancellationToken);

    if (string.IsNullOrEmpty(settings.ModuleSettings)) { return new Dictionary<string, object>(); }

    try {
      var allSettings = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(settings.ModuleSettings);

      return allSettings?.TryGetValue(module, out var moduleSettings) == true ? moduleSettings : new Dictionary<string, object>();
    }
    catch (Exception ex) {
      logger.LogWarning(ex, "Failed to deserialize module settings for tenant {TenantId}, module {Module}", tenantId, module);

      return new Dictionary<string, object>();
    }
  }

  // === UTILITY METHODS ===

  public async Task<bool> TenantSettingsExistAsync(Guid? tenantId, CancellationToken cancellationToken = default) { return await context.TenantSettings.AnyAsync(s => s.TenantId == tenantId && s.DeletedAt == null, cancellationToken); }

  public async Task<TenantSettings> GetEffectiveTenantSettingsAsync(Guid tenantId, CancellationToken cancellationToken = default) {
    // First try to get tenant-specific settings
    var tenantSettings = await context.TenantSettings.Include(s => s.Tenant).FirstOrDefaultAsync(s => s.TenantId == tenantId && s.DeletedAt == null, cancellationToken);

    if (tenantSettings != null) { return tenantSettings; }

    // Fall back to global default settings
    var globalSettings = await GetTenantSettingsAsync(null, cancellationToken);

    // Create a copy for this tenant (don't modify the global settings)
    var effectiveSettings = CloneSettings(globalSettings);
    effectiveSettings.TenantId = tenantId;

    // Load tenant reference
    var tenant = await context.Tenants.FindAsync([tenantId], cancellationToken);
    effectiveSettings.Tenant = tenant;

    return effectiveSettings;
  }

  public async Task<TenantSettings> CloneTenantSettingsAsync(Guid? sourceTenantId, Guid targetTenantId, CancellationToken cancellationToken = default) {
    var sourceSettings = await GetTenantSettingsAsync(sourceTenantId, cancellationToken);
    var clonedSettings = CloneSettings(sourceSettings);

    clonedSettings.TenantId = targetTenantId;
    clonedSettings.Id = Guid.NewGuid(); // New ID for cloned settings
    clonedSettings.CreatedAt = DateTime.UtcNow;
    clonedSettings.UpdatedAt = DateTime.UtcNow;
    clonedSettings.DeletedAt = null;
    clonedSettings.Version = 1;

    // Load target tenant
    var targetTenant = await context.Tenants.FindAsync([targetTenantId], cancellationToken);

    if (targetTenant == null) { throw new ArgumentException($"Target tenant with ID {targetTenantId} not found", nameof(targetTenantId)); }

    clonedSettings.Tenant = targetTenant;

    return await SaveTenantSettingsAsync(targetTenantId, clonedSettings, cancellationToken);
  }

  // === PRIVATE HELPER METHODS ===

  private static void CopySettingsProperties(TenantSettings source, TenantSettings target) {
    // Copy all properties except navigation properties and audit fields
    target.DefaultLanguage = source.DefaultLanguage;
    target.DefaultTimezone = source.DefaultTimezone;
    target.DateFormat = source.DateFormat;
    target.Use24HourFormat = source.Use24HourFormat;
    target.DefaultCurrency = source.DefaultCurrency;
    target.PrimaryColor = source.PrimaryColor;
    target.SecondaryColor = source.SecondaryColor;
    target.LogoUrl = source.LogoUrl;
    target.CustomCss = source.CustomCss;
    target.DefaultTheme = source.DefaultTheme;
    target.FeatureFlags = source.FeatureFlags;
    target.ModuleSettings = source.ModuleSettings;
    target.AllowUserRegistration = source.AllowUserRegistration;
    target.RequireRegistrationApproval = source.RequireRegistrationApproval;
    target.EnableEmailNotifications = source.EnableEmailNotifications;
    target.EnablePushNotifications = source.EnablePushNotifications;
    target.EnableSmsNotifications = source.EnableSmsNotifications;
    target.DefaultNotificationEmail = source.DefaultNotificationEmail;
    target.RequireTwoFactorAuth = source.RequireTwoFactorAuth;
    target.MinPasswordLength = source.MinPasswordLength;
    target.PasswordComplexityRules = source.PasswordComplexityRules;
    target.SessionTimeoutMinutes = source.SessionTimeoutMinutes;
    target.MaxUsers = source.MaxUsers;
    target.StorageQuotaMB = source.StorageQuotaMB;
    target.SubscriptionPlan = source.SubscriptionPlan;
    target.SubscriptionExpiresAt = source.SubscriptionExpiresAt;
    target.SupportEmail = source.SupportEmail;
    target.SupportPhone = source.SupportPhone;
    target.Address = source.Address;
  }

  private static TenantSettings CloneSettings(TenantSettings source) {
    var clone = TenantSettings.CreateDefault();
    CopySettingsProperties(source, clone);

    return clone;
  }
}
