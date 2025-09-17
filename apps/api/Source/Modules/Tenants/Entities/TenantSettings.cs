using GameGuild.Modules.Resources;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// Tenant-specific settings and configuration
/// Provides tenant-level customization for features, UI, localization, and business logic
/// </summary>
[Table("TenantSettings")]
[Index(nameof(TenantId), IsUnique = true)]
public class TenantSettings : Resource
{
    /// <summary>
    /// Reference to the tenant (null for global default settings)
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Navigation property to the tenant
    /// </summary>
    public new virtual Tenant? Tenant { get; set; }

    // === LOCALIZATION SETTINGS ===

    /// <summary>
    /// Default language/culture for the tenant (e.g., "en-US", "pt-BR")
    /// </summary>
    [MaxLength(10)]
    public string DefaultLanguage { get; set; } = "en-US";

    /// <summary>
    /// Default timezone for the tenant (e.g., "UTC", "America/New_York")
    /// </summary>
    [MaxLength(50)]
    public string DefaultTimezone { get; set; } = "UTC";

    /// <summary>
    /// Date format preference (e.g., "MM/dd/yyyy", "dd/MM/yyyy")
    /// </summary>
    [MaxLength(20)]
    public string DateFormat { get; set; } = "MM/dd/yyyy";

    /// <summary>
    /// Time format preference (12-hour vs 24-hour)
    /// </summary>
    public bool Use24HourFormat { get; set; } = false;

    /// <summary>
    /// Default currency code (ISO 4217)
    /// </summary>
    [MaxLength(3)]
    public string DefaultCurrency { get; set; } = "USD";

    // === UI/BRANDING SETTINGS ===

    /// <summary>
    /// Primary brand color (hex code)
    /// </summary>
    [MaxLength(7)]
    public string? PrimaryColor { get; set; }

    /// <summary>
    /// Secondary brand color (hex code)
    /// </summary>
    [MaxLength(7)]
    public string? SecondaryColor { get; set; }

    /// <summary>
    /// Tenant logo URL or base64 data
    /// </summary>
    [MaxLength(2000)]
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Custom CSS/styling overrides
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? CustomCss { get; set; }

    /// <summary>
    /// Default theme ("light", "dark", "auto")
    /// </summary>
    [MaxLength(10)]
    public string DefaultTheme { get; set; } = "auto";

    // === FEATURE FLAGS/SETTINGS ===

    /// <summary>
    /// JSON configuration for tenant-specific feature flags
    /// </summary>
    [Column(TypeName = "JSON")]
    public string? FeatureFlags { get; set; } = "{}";

    /// <summary>
    /// JSON configuration for module-specific settings
    /// </summary>
    [Column(TypeName = "JSON")]
    public string? ModuleSettings { get; set; } = "{}";

    /// <summary>
    /// Enable/disable user self-registration for this tenant
    /// </summary>
    public bool AllowUserRegistration { get; set; } = true;

    /// <summary>
    /// Require admin approval for new user registrations
    /// </summary>
    public bool RequireRegistrationApproval { get; set; } = false;

    // === NOTIFICATION SETTINGS ===

    /// <summary>
    /// Enable email notifications
    /// </summary>
    public bool EnableEmailNotifications { get; set; } = true;

    /// <summary>
    /// Enable push notifications
    /// </summary>
    public bool EnablePushNotifications { get; set; } = true;

    /// <summary>
    /// Enable SMS notifications
    /// </summary>
    public bool EnableSmsNotifications { get; set; } = false;

    /// <summary>
    /// Default notification email address
    /// </summary>
    [MaxLength(255)]
    public string? DefaultNotificationEmail { get; set; }

    // === SECURITY SETTINGS ===

    /// <summary>
    /// Require two-factor authentication for all users
    /// </summary>
    public bool RequireTwoFactorAuth { get; set; } = false;

    /// <summary>
    /// Password minimum length requirement
    /// </summary>
    public int MinPasswordLength { get; set; } = 8;

    /// <summary>
    /// Password complexity requirements (JSON)
    /// </summary>
    [Column(TypeName = "JSON")]
    public string? PasswordComplexityRules { get; set; } = """
        {
            "requireUppercase": true,
            "requireLowercase": true,
            "requireDigits": true,
            "requireSpecialChars": true
        }
        """;

    /// <summary>
    /// Session timeout in minutes
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 480; // 8 hours

    // === BUSINESS SETTINGS ===

    /// <summary>
    /// Maximum number of users allowed for this tenant (null = unlimited)
    /// </summary>
    public int? MaxUsers { get; set; }

    /// <summary>
    /// Storage quota in MB (null = unlimited)
    /// </summary>
    public long? StorageQuotaMB { get; set; }

    /// <summary>
    /// Subscription plan/tier
    /// </summary>
    [MaxLength(50)]
    public string? SubscriptionPlan { get; set; }

    /// <summary>
    /// Subscription expires at (null = no expiration)
    /// </summary>
    public DateTime? SubscriptionExpiresAt { get; set; }

    // === CONTACT INFORMATION ===

    /// <summary>
    /// Support email for this tenant
    /// </summary>
    [MaxLength(255)]
    public string? SupportEmail { get; set; }

    /// <summary>
    /// Support phone number
    /// </summary>
    [MaxLength(20)]
    public string? SupportPhone { get; set; }

    /// <summary>
    /// Physical address (JSON structure)
    /// </summary>
    [Column(TypeName = "JSON")]
    public string? Address { get; set; }

    // === HELPER METHODS ===

    /// <summary>
    /// Get feature flag value as boolean
    /// </summary>
    public bool GetFeatureFlag(string key, bool defaultValue = false)
    {
        if (string.IsNullOrEmpty(FeatureFlags)) return defaultValue;

        try
        {
            var flags = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(FeatureFlags);
            return flags?.TryGetValue(key, out var value) == true &&
                   value is bool boolValue ? boolValue : defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Set feature flag value
    /// </summary>
    public void SetFeatureFlag(string key, bool value)
    {
        var flags = string.IsNullOrEmpty(FeatureFlags) ?
            new Dictionary<string, object>() :
            System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(FeatureFlags) ??
            new Dictionary<string, object>();

        flags[key] = value;
        FeatureFlags = System.Text.Json.JsonSerializer.Serialize(flags);
        Touch();
    }

    /// <summary>
    /// Get module setting value
    /// </summary>
    public T? GetModuleSetting<T>(string module, string key, T? defaultValue = default)
    {
        if (string.IsNullOrEmpty(ModuleSettings)) return defaultValue;

        try
        {
            var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(ModuleSettings);
            return settings?.TryGetValue(module, out var moduleDict) == true &&
                   moduleDict.TryGetValue(key, out var value) &&
                   value is T typedValue ? typedValue : defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Set module setting value
    /// </summary>
    public void SetModuleSetting<T>(string module, string key, T value)
    {
        var settings = string.IsNullOrEmpty(ModuleSettings) ?
            new Dictionary<string, Dictionary<string, object>>() :
            System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(ModuleSettings) ??
            new Dictionary<string, Dictionary<string, object>>();

        if (!settings.ContainsKey(module))
        {
            settings[module] = new Dictionary<string, object>();
        }

        settings[module][key] = value!;
        ModuleSettings = System.Text.Json.JsonSerializer.Serialize(settings);
        Touch();
    }

    /// <summary>
    /// Create default settings for a tenant
    /// </summary>
    public static TenantSettings CreateDefault(Guid? tenantId = null)
    {
        return new TenantSettings
        {
            TenantId = tenantId,
            DefaultLanguage = "en-US",
            DefaultTimezone = "UTC",
            DateFormat = "MM/dd/yyyy",
            Use24HourFormat = false,
            DefaultCurrency = "USD",
            DefaultTheme = "auto",
            AllowUserRegistration = true,
            RequireRegistrationApproval = false,
            EnableEmailNotifications = true,
            EnablePushNotifications = true,
            EnableSmsNotifications = false,
            RequireTwoFactorAuth = false,
            MinPasswordLength = 8,
            SessionTimeoutMinutes = 480,
            FeatureFlags = "{}",
            ModuleSettings = "{}"
        };
    }
}
