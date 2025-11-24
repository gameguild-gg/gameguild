using GameGuild.Tenants.Entities;

namespace GameGuild.Tenants.Abstractions;

/// <summary>
///     Repository interface for tenant member data access operations
/// </summary>
public interface ITenantMemberRepository
{
    /// <summary>
    ///     Get tenant member by ID
    /// </summary>
    /// <param name="id">The member ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant member or null if not found</returns>
    Task<TenantMember?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get tenant members by tenant ID
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="includeInactive">Include inactive members</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tenant members</returns>
    Task<IReadOnlyList<TenantMember>> GetByTenantIdAsync(Guid tenantId, bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get tenant member by user ID and tenant ID
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant member or null if not found</returns>
    Task<TenantMember?> GetByUserAndTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Create a new tenant member
    /// </summary>
    /// <param name="member">The tenant member to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created tenant member</returns>
    Task<TenantMember> CreateAsync(TenantMember member, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update an existing tenant member
    /// </summary>
    /// <param name="member">The tenant member to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant member</returns>
    Task<TenantMember> UpdateAsync(TenantMember member, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete a tenant member
    /// </summary>
    /// <param name="id">The member ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if user exists in tenant
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user is a member of the tenant</returns>
    Task<bool> ExistsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get members with pagination
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="includeInactive">Include inactive members</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of members</returns>
    Task<(IReadOnlyList<TenantMember> Members, int TotalCount)> GetPagedAsync(Guid tenantId, int page, int pageSize, bool includeInactive = false, CancellationToken cancellationToken = default);
}
