namespace GameGuild.Authentication.Models;

/// <summary>
///     Represents tenant-specific information for authentication context.
/// </summary>
public abstract class TenantInfo
{
    /// <summary>
    ///     Tenant unique identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    ///     Tenant name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Tenant slug/identifier.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    ///     Whether the tenant is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    ///     Tenant subscription tier or plan.
    /// </summary>
    public string? SubscriptionTier { get; set; }

    /// <summary>
    ///     Additional tenant metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
