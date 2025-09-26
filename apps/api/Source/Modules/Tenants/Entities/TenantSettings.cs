using GameGuild.Modules.Resources;

namespace GameGuild.Modules.Tenants;

/// <summary> Tenant-specific settings and configuration Provides tenant-level customization for features, UI, localization, and business logic </summary>
[Table("TenantSettings")]
[Index(nameof(TenantId), IsUnique = true)]
public class TenantSettings : Resource
{
    /// <summary> Reference to the tenant (null for global default settings) </summary>
    public Guid? TenantId { get; set; }

    /// <summary> Navigation property to the tenant </summary>
    public new virtual Tenant? Tenant { get; set; }

    /// <summary> Default language/culture for the tenant (e.g., "en-US", "pt-BR") </summary>
    [MaxLength(10)]
    public string DefaultLanguage { get; set; } = "en-US";

    /// <summary> Default timezone for the tenant (e.g., "UTC", "America/New_York") </summary>
    [MaxLength(50)]
    public string DefaultTimezone { get; set; } = "UTC";

    /// <summary> Enable/disable user self-registration for this tenant </summary>
    public bool AllowUserRegistration { get; set; } = true;

    /// <summary> Require admin approval for new user registrations </summary>
    public bool RequireRegistrationApproval { get; set; }

    /// <summary>
    /// Creates default tenant settings for a given tenant
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
            AllowUserRegistration = true,
            RequireRegistrationApproval = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
