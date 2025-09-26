namespace GameGuild.Modules.Tenants;

/// <summary>
/// Provides access to the current tenant context for the request
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant for the request
    /// </summary>
    Tenant? CurrentTenant { get; }

    /// <summary>
    /// Gets the current tenant ID for the request
    /// </summary>
    Guid? CurrentTenantId { get; }

    /// <summary>
    /// Sets the current tenant for the request
    /// </summary>
    /// <param name="tenant">The tenant to set as current</param>
    void SetCurrentTenant(Tenant? tenant);
}
