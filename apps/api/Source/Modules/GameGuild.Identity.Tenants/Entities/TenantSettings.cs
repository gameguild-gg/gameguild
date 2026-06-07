using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Tenant-specific settings and configuration
///     Provides tenant-level customization for features, UI, localization, and business logic
/// </summary>
[Table("TenantSettings")]
[Index(nameof(TenantId), IsUnique = true)]
public class TenantSettings : EntityBase, ITenantable
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public TenantSettings() { }

    /// <summary>
    ///     Constructor for partial initialization
    /// </summary>
    /// <param name="partial">Partial tenant settings data</param>
    public TenantSettings(object partial) : base(partial) { }

    /// <summary>
    ///     ID of the tenant these settings belong to
    /// </summary>
    [Required]
    public new Guid TenantId { get; set; }

    /// <summary>
    ///     Default language code for the tenant (e.g., "en-US", "pt-BR")
    /// </summary>
    [MaxLength(10)]
    public string DefaultLanguage { get; set; } = "en-US";

    /// <summary>
    ///     Default timezone for the tenant (e.g., "UTC", "America/New_York")
    /// </summary>
    [MaxLength(50)]
    public string DefaultTimezone { get; set; } = "UTC";

    /// <summary>
    ///     Default currency code for the tenant (e.g., "USD", "BRL")
    /// </summary>
    [MaxLength(3)]
    public string DefaultCurrency { get; set; } = "USD";

    /// <summary>
    ///     Enable/disable user self-registration for this tenant
    /// </summary>
    public bool AllowUserRegistration { get; set; } = true;

    /// <summary>
    ///     Require admin approval for new user registrations
    /// </summary>
    public bool RequireRegistrationApproval { get; set; }

    /// <summary>
    ///     Enable/disable two-factor authentication requirement
    /// </summary>
    public bool RequireTwoFactorAuth { get; set; }

    /// <summary>
    ///     Maximum number of users allowed for this tenant (null = unlimited)
    /// </summary>
    public int? MaxUsers { get; set; }

    /// <summary>
    ///     Maximum storage quota in bytes (null = unlimited)
    /// </summary>
    public long? StorageQuota { get; set; }

    /// <summary>
    ///     Enable/disable audit logging for this tenant
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>
    ///     Enable/disable API access for this tenant
    /// </summary>
    public bool EnableApiAccess { get; set; } = true;

    /// <summary>
    ///     Custom branding settings (JSON)
    /// </summary>
    [MaxLength(5000)]
    public string? BrandingSettings { get; set; }

    /// <summary>
    ///     Notification settings (JSON)
    /// </summary>
    [MaxLength(5000)]
    public string? NotificationSettings { get; set; }

    /// <summary>
    ///     Security settings (JSON)
    /// </summary>
    [MaxLength(5000)]
    public string? SecuritySettings { get; set; }

    /// <summary>
    ///     Integration settings (JSON)
    ///     Stores external service configuration, API keys, and SSO settings.
    /// </summary>
    public string? IntegrationSettingsJson { get; set; }

    /// <summary>
    ///     Navigation property to the tenant
    /// </summary>
    public Tenant? Tenant { get; set; }

    /// <summary>
    ///     Creates default tenant settings for a given tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>TenantSettings with default values</returns>
    public static TenantSettings CreateDefault(Guid tenantId)
    {
        return new TenantSettings
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DefaultLanguage = "en-US",
            DefaultTimezone = "UTC",
            DefaultCurrency = "USD",
            AllowUserRegistration = true,
            RequireRegistrationApproval = false,
            RequireTwoFactorAuth = false,
            EnableAuditLogging = true,
            EnableApiAccess = true
        };
    }

    /// <summary>
    ///     Update language settings
    /// </summary>
    public void UpdateLanguageSettings(string language, string timezone, string currency)
    {
        DefaultLanguage = language;
        DefaultTimezone = timezone;
        DefaultCurrency = currency;
        Touch();
    }

    /// <summary>
    ///     Update security settings
    /// </summary>
    public void UpdateSecuritySettings(bool requireTwoFactor, bool requireApproval)
    {
        RequireTwoFactorAuth = requireTwoFactor;
        RequireRegistrationApproval = requireApproval;
        Touch();
    }

    /// <summary>
    ///     Update quota settings
    /// </summary>
    public void UpdateQuotaSettings(int? maxUsers, long? storageQuota)
    {
        MaxUsers = maxUsers;
        StorageQuota = storageQuota;
        Touch();
    }
}
