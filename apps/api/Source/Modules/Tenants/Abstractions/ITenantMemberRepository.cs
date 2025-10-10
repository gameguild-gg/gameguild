namespace GameGuild.Modules.Tenants;

/// <summary>
///     Repository interface for tenant member data access operations
/// </summary>
public interface ITenantMemberRepository
{
    /// <summary>
    ///     Get all members of a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tenant members</returns>
    Task<IReadOnlyList<TenantMember>> GetMembersByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all tenants a user is a member of
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tenant memberships</returns>
    Task<IReadOnlyList<TenantMember>> GetTenantsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get a specific tenant member
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tenant member or null if not found</returns>
    Task<TenantMember?> GetMemberAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if a user is a member of a tenant
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user is a member</returns>
    Task<bool> IsMemberOfTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get the role of a user in a tenant
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user's role or null if not a member</returns>
    Task<string?> GetMemberRoleAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Add a member to a tenant
    /// </summary>
    /// <param name="member">The tenant member to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added tenant member</returns>
    Task<TenantMember> AddMemberAsync(TenantMember member, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update a tenant member
    /// </summary>
    /// <param name="member">The tenant member to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated tenant member</returns>
    Task<TenantMember> UpdateMemberAsync(TenantMember member, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Remove a member from a tenant
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveMemberAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get active members of a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active tenant members</returns>
    Task<IReadOnlyList<TenantMember>> GetActiveMembersAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get members by role in a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="role">The role to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tenant members with the specified role</returns>
    Task<IReadOnlyList<TenantMember>> GetMembersByRoleAsync(Guid tenantId, string role, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get direct child members of a parent member
    /// </summary>
    /// <param name="parentMemberId">The parent member ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of child tenant members</returns>
    Task<IReadOnlyList<TenantMember>> GetChildMembersAsync(Guid parentMemberId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get the complete hierarchy for a member (all descendants)
    /// </summary>
    /// <param name="memberId">The member ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all descendant tenant members</returns>
    Task<IReadOnlyList<TenantMember>> GetMemberHierarchyAsync(Guid memberId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all root members (no parent) for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of root tenant members</returns>
    Task<IReadOnlyList<TenantMember>> GetRootMembersAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update the hierarchy path for a member and all descendants
    /// </summary>
    /// <param name="memberId">The member ID</param>
    /// <param name="hierarchyPath">The new hierarchy path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateHierarchyPathAsync(Guid memberId, string hierarchyPath, CancellationToken cancellationToken = default);
}
