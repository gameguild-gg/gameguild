using GameGuild.Database;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Service implementation for managing tenants
/// </summary>
public class TenantService(ApplicationDbContext context, IPermissionService permissionService) : ITenantService
{
  /// <summary>
  /// Get all tenants
  /// </summary>
  /// <returns>List of tenants</returns>
  public async Task<IEnumerable<Tenant>> GetAllTenantsAsync() { return await context.Tenants.Include(t => t.TenantPermissions).ToListAsync(); }

  /// <summary>
  /// Get a specific tenant by ID
  /// </summary>
  /// <param name="id">Tenant ID</param>
  /// <returns>Tenant or null if not found</returns>
  public async Task<Tenant?> GetTenantByIdAsync(Guid id)
  {
    return await context.Tenants.Include(t => t.TenantPermissions)
                        .ThenInclude(tp => tp.User)
                        .FirstOrDefaultAsync(t => t.Id == id);
  }

  /// <summary>
  /// Get a tenant by name
  /// </summary>
  /// <param name="name">Tenant name</param>
  /// <returns>Tenant or null if not found</returns>
  public async Task<Tenant?> GetTenantByNameAsync(string name) { return await context.Tenants.Include(t => t.TenantPermissions).FirstOrDefaultAsync(t => t.Name == name); }

  /// <summary>
  /// Create a new tenant
  /// </summary>
  /// <param name="tenant">Tenant to create</param>
  /// <returns>Created tenant</returns>
  public async Task<Tenant> CreateTenantAsync(Tenant tenant)
  {
    context.Tenants.Add(tenant);
    await context.SaveChangesAsync();

    return tenant;
  }

  /// <summary>
  /// Update an existing tenant
  /// </summary>
  /// <param name="tenant">Tenant to update</param>
  /// <returns>Updated tenant</returns>
  public async Task<Tenant> UpdateTenantAsync(Tenant tenant)
  {
    var existingTenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenant.Id);

    if (existingTenant == null) throw new InvalidOperationException($"Tenant with ID {tenant.Id} not found");

    // Update properties
    existingTenant.Name = tenant.Name;
    existingTenant.Description = tenant.Description;
    existingTenant.IsActive = tenant.IsActive;
    existingTenant.Touch(); // Update timestamp

    await context.SaveChangesAsync();

    return existingTenant;
  }

  /// <summary>
  /// Soft delete a tenant
  /// </summary>
  /// <param name="id">Tenant ID to delete</param>
  /// <returns>True if deleted successfully</returns>
  public async Task<bool> SoftDeleteTenantAsync(Guid id)
  {
    var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == id);

    if (tenant == null) return false;

    tenant.SoftDelete();
    await context.SaveChangesAsync();

    return true;
  }

  /// <summary>
  /// Restore a soft-deleted tenant
  /// </summary>
  /// <param name="id">Tenant ID to restore</param>
  /// <returns>True if restored successfully</returns>
  public async Task<bool> RestoreTenantAsync(Guid id)
  {
    var tenant = await context.Tenants.IgnoreQueryFilters()
                              .FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt != null);

    if (tenant == null) return false;

    tenant.Restore();
    await context.SaveChangesAsync();

    return true;
  }

  /// <summary>
  /// Permanently delete a tenant
  /// </summary>
  /// <param name="id">Tenant ID to delete</param>
  /// <returns>True if deleted successfully</returns>
  public async Task<bool> HardDeleteTenantAsync(Guid id)
  {
    var tenant = await context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);

    if (tenant == null) return false;

    context.Tenants.Remove(tenant);
    await context.SaveChangesAsync();

    return true;
  }

  /// <summary>
  /// Activate a tenant
  /// </summary>
  /// <param name="id">Tenant ID</param>
  /// <returns>True if activated successfully</returns>
  public async Task<bool> ActivateTenantAsync(Guid id)
  {
    var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == id);

    if (tenant == null) return false;

    tenant.Activate();
    await context.SaveChangesAsync();

    return true;
  }

  /// <summary>
  /// Deactivate a tenant
  /// </summary>
  /// <param name="id">Tenant ID</param>
  /// <returns>True if deactivated successfully</returns>
  public async Task<bool> DeactivateTenantAsync(Guid id)
  {
    var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == id);

    if (tenant == null) return false;

    tenant.Deactivate();
    await context.SaveChangesAsync();

    return true;
  }

  /// <summary>
  /// Get soft-deleted tenants
  /// </summary>
  /// <returns>List of soft-deleted tenants</returns>
  public async Task<IEnumerable<Tenant>> GetDeletedTenantsAsync()
  {
    return await context.Tenants.IgnoreQueryFilters()
                        .Where(t => t.DeletedAt != null)
                        .Include(t => t.TenantPermissions)
                        .ToListAsync();
  }

  /// <summary>
  /// Add a user to a tenant
  /// </summary>
  /// <param name="userId">User ID</param>
  /// <param name="tenantId">Tenant ID</param>
  /// <returns>Created TenantPermission relationship</returns>
  public async Task<TenantPermission> AddUserToTenantAsync(Guid userId, Guid tenantId)
  {
    // Use the permission service to handle tenant membership
    return await permissionService.JoinTenantAsync(userId, tenantId);
  }

  /// <summary>
  /// Remove a user from a tenant
  /// </summary>
  /// <param name="userId">User ID</param>
  /// <param name="tenantId">Tenant ID</param>
  /// <returns>True if removed successfully</returns>
  public async Task<bool> RemoveUserFromTenantAsync(Guid userId, Guid tenantId)
  {
    await permissionService.LeaveTenantAsync(userId, tenantId);

    return true;
  }

  /// <summary>
  /// Get users in a tenant
  /// </summary>
  /// <param name="tenantId">Tenant ID</param>
  /// <returns>List of TenantPermission relationships</returns>
  public async Task<IEnumerable<TenantPermission>> GetUsersInTenantAsync(Guid tenantId)
  {
    return await context.TenantPermissions.Where(tp => tp.TenantId == tenantId && tp.UserId != null)
                        .Include(tp => tp.User)
                        .Include(tp => tp.Tenant)
                        .ToListAsync();
  }

  /// <summary>
  /// Get tenants for a user
  /// </summary>
  /// <param name="userId">User ID</param>
  /// <returns>List of TenantPermission relationships</returns>
  public async Task<IEnumerable<TenantPermission>> GetTenantsForUserAsync(Guid userId) { return await permissionService.GetUserTenantsAsync(userId); }

  // === DEFAULT TENANT FUNCTIONALITY ===

  /// <summary>
  /// Get the default tenant (creates one if none exists)
  /// </summary>
  /// <returns>Default tenant</returns>
  public async Task<Tenant> GetOrCreateDefaultTenantAsync()
  {
    var defaultTenant = await context.Tenants
        .FirstOrDefaultAsync(t => t.IsDefault && t.DeletedAt == null);

    if (defaultTenant != null)
    {
      return defaultTenant;
    }

    // No default tenant exists, create one
    defaultTenant = new Tenant
    {
      Name = "Default Organization",
      Description = "Default tenant for users without specific tenant assignment",
      Slug = "default",
      AdminEmail = "admin@default.local",
      IsActive = true,
      IsDefault = true
    };

    context.Tenants.Add(defaultTenant);
    await context.SaveChangesAsync();

    return defaultTenant;
  }

  /// <summary>
  /// Get the current default tenant
  /// </summary>
  /// <returns>Default tenant or null if none exists</returns>
  public async Task<Tenant?> GetDefaultTenantAsync()
  {
    return await context.Tenants
        .FirstOrDefaultAsync(t => t.IsDefault && t.DeletedAt == null);
  }

  /// <summary>
  /// Set a tenant as the default tenant
  /// </summary>
  /// <param name="tenantId">Tenant ID to set as default</param>
  /// <returns>Updated tenant</returns>
  public async Task<Tenant> SetDefaultTenantAsync(Guid tenantId)
  {
    // First, unset any existing default tenant
    var currentDefault = await context.Tenants
        .FirstOrDefaultAsync(t => t.IsDefault && t.DeletedAt == null);

    if (currentDefault != null)
    {
      currentDefault.IsDefault = false;
      currentDefault.Touch();
    }

    // Set the new default tenant
    var newDefault = await context.Tenants
        .FirstOrDefaultAsync(t => t.Id == tenantId && t.DeletedAt == null);

    if (newDefault == null)
    {
      throw new InvalidOperationException($"Tenant with ID {tenantId} not found");
    }

    newDefault.IsDefault = true;
    newDefault.Touch();

    await context.SaveChangesAsync();

    return newDefault;
  }

  /// <summary>
  /// Check if a tenant is the default tenant
  /// </summary>
  /// <param name="tenantId">Tenant ID to check</param>
  /// <returns>True if this is the default tenant</returns>
  public async Task<bool> IsDefaultTenantAsync(Guid tenantId)
  {
    return await context.Tenants
        .AnyAsync(t => t.Id == tenantId && t.IsDefault && t.DeletedAt == null);
  }

  /// <summary>
  /// Get effective tenant (returns specified tenant or default if null)
  /// </summary>
  /// <param name="tenantId">Tenant ID (null to get default)</param>
  /// <returns>Effective tenant</returns>
  public async Task<Tenant> GetEffectiveTenantAsync(Guid? tenantId)
  {
    if (tenantId.HasValue)
    {
      var tenant = await GetTenantByIdAsync(tenantId.Value);
      if (tenant != null)
      {
        return tenant;
      }
    }

    // Fall back to default tenant
    return await GetOrCreateDefaultTenantAsync();
  }
}
