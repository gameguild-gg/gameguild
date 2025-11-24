using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Abstractions;

/// <summary>
///     Interface for tenant management operations
/// </summary>
public interface ITenantService
{
    /// <summary>
    ///     Get all active tenants
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active tenants</returns>
    Task<IReadOnlyList<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get tenant by ID
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant or null if not found</returns>
    Task<Tenant?> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get tenant by slug
    /// </summary>
    /// <param name="slug">The tenant slug</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant or null if not found</returns>
    Task<Tenant?> GetTenantBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get default tenant
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The default tenant or null if not found</returns>
    Task<Tenant?> GetDefaultTenantAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Create a new tenant
    /// </summary>
    /// <param name="name">The tenant name</param>
    /// <param name="slug">The tenant slug</param>
    /// <param name="description">The tenant description</param>
    /// <param name="adminEmail">The admin email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created tenant</returns>
    Task<Tenant> CreateTenantAsync(string name, string slug, string? description = null, string? adminEmail = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update an existing tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="name">The new name</param>
    /// <param name="description">The new description</param>
    /// <param name="adminEmail">The new admin email</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant</returns>
    Task<Tenant> UpdateTenantAsync(Guid tenantId, string name, string? description = null, string? adminEmail = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete a tenant (soft delete)
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Activate a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The activated tenant</returns>
    Task<Tenant> ActivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deactivate a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The deactivated tenant</returns>
    Task<Tenant> DeactivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Archive a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="reason">Reason for archiving</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The archived tenant</returns>
    Task<Tenant> ArchiveTenantAsync(Guid tenantId, string reason = "", CancellationToken cancellationToken = default);

    /// <summary>
    ///     Restore an archived tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The restored tenant</returns>
    Task<Tenant> RestoreTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if a tenant slug is available
    /// </summary>
    /// <param name="slug">The slug to check</param>
    /// <param name="excludeId">Optional tenant ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the slug is available</returns>
    Task<bool> IsSlugAvailableAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get tenants with pagination
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="includeArchived">Include archived tenants</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of tenants</returns>
    Task<(IReadOnlyList<Tenant> Tenants, int TotalCount)> GetTenantsPagedAsync(int page, int pageSize, bool includeArchived = false, CancellationToken cancellationToken = default);
}
