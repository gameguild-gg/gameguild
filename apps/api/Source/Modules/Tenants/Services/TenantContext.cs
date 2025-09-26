namespace GameGuild.Modules.Tenants;

/// <summary>
///     Implementation of tenant context that provides access to the current tenant for the request
/// </summary>
public class TenantContext : ITenantContext
{
    /// <inheritdoc />
    public Tenant? CurrentTenant { get; private set; }

    /// <inheritdoc />
    public Guid? CurrentTenantId { get => CurrentTenant?.Id; }

    /// <inheritdoc />
    public void SetCurrentTenant(Tenant? tenant) { CurrentTenant = tenant; }
}
