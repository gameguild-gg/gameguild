namespace GameGuild.Modules.Tenants;

/// <summary>
/// Implementation of tenant context that provides access to the current tenant for the request
/// </summary>
public class TenantContext : ITenantContext
{
    private Tenant? _currentTenant;

    /// <inheritdoc />
    public Tenant? CurrentTenant => _currentTenant;

    /// <inheritdoc />
    public Guid? CurrentTenantId => _currentTenant?.Id;

    /// <inheritdoc />
    public void SetCurrentTenant(Tenant? tenant)
    {
        _currentTenant = tenant;
    }
}