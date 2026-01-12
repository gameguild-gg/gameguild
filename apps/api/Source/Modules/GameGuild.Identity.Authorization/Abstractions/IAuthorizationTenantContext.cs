namespace GameGuild.Identity.Authorization;

/// <summary>
///     Provides the current tenant context for authorization decisions.
/// </summary>
public interface IAuthorizationTenantContext
{
    /// <summary>
    ///     Gets the current tenant ID.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    ///     Gets whether a tenant has been resolved.
    /// </summary>
    bool HasTenant => !string.IsNullOrEmpty(TenantId);
}
