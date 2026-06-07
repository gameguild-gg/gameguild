
namespace GameGuild.Identity.Tenants;

/// <summary>
///     Repository interface for tenant operations
/// </summary>
public interface ITenantRepository
{
    /// <summary>
    ///     Get tenant by ID
    /// </summary>
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get tenant by slug
    /// </summary>
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if slug is unique (excluding specific tenant)
    /// </summary>
    Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all active tenants
    /// </summary>
    Task<IEnumerable<Tenant>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all tenants
    /// </summary>
    Task<IEnumerable<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get tenants with pagination
    /// </summary>
    Task<(IEnumerable<Tenant> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        bool? isActive = null,
        bool? isArchived = null,
        string? searchTerm = null,
        string? sortBy = "Name",
        bool sortDescending = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Create a new tenant
    /// </summary>
    Task<Tenant> CreateAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update an existing tenant
    /// </summary>
    Task<Tenant> UpdateAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete a tenant (soft delete by ID)
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete a tenant (soft delete by entity)
    /// </summary>
    Task DeleteAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get queryable for advanced filtering and LINQ operations
    /// </summary>
    Task<IQueryable<Tenant>> GetQueryableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get audit log entries for a tenant with filtering and pagination
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <param name="action">Optional action type filter</param>
    /// <param name="actorId">Optional actor ID filter</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated audit log entries</returns>
    Task<PagedResult<TenantAuditLogEntry>> GetAuditLogAsync(
        Guid tenantId,
        DateTime? startDate,
        DateTime? endDate,
        string? action,
        Guid? actorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
