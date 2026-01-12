namespace GameGuild.Identity.Authorization;

/// <summary>
///     Repository interface for managing Access Control List entries.
/// </summary>
public interface IAccessControlListEntryRepository
{
    /// <summary>
    ///     Gets an Access Control List entry by its ID.
    /// </summary>
    /// <param name="id">The Access Control List entry ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Access Control List entry, or null if not found.</returns>
    Task<AccessControlListEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the Access Control List entry for a specific principal and resource.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="principalType">The principal type.</param>
    /// <param name="principalId">The principal ID (null for Anonymous).</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Access Control List entry, or null if not found.</returns>
    Task<AccessControlListEntry?> GetByPrincipalAndResourceAsync(
        Guid tenantId,
        AclPrincipalType principalType,
        Guid? principalId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all Access Control List entries for a resource that match any of the given principals.
    ///     Used for deny-first evaluation across user, role, group, and anonymous principals.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="principals">The list of principals to match (type, id).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All matching Access Control List entries.</returns>
    Task<IReadOnlyList<AccessControlListEntry>> GetByResourceAndPrincipalsAsync(
        Guid tenantId,
        string resourceType,
        string resourceId,
        IEnumerable<(AclPrincipalType Type, Guid? Id)> principals,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the Access Control List entry for a specific user and resource.
    ///     For backward compatibility - use GetByPrincipalAndResourceAsync for new code.
    /// </summary>
    Task<AccessControlListEntry?> GetByUserAndResourceAsync(
        Guid tenantId,
        Guid userId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all Access Control List entries for a user within a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All Access Control List entries for the user.</returns>
    Task<IReadOnlyList<AccessControlListEntry>> GetByUserAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all Access Control List entries for a specific resource.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All Access Control List entries for the resource.</returns>
    Task<IReadOnlyList<AccessControlListEntry>> GetByResourceAsync(
        Guid tenantId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all Access Control List entries for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All Access Control List entries for the tenant.</returns>
    Task<IReadOnlyList<AccessControlListEntry>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets expired Access Control List entries that need cleanup.
    /// </summary>
    /// <param name="cutoffDate">The cutoff date for expiration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Expired Access Control List entries.</returns>
    Task<IReadOnlyList<AccessControlListEntry>> GetExpiredEntriesAsync(
        DateTime cutoffDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds a new Access Control List entry.
    /// </summary>
    /// <param name="entry">The Access Control List entry to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(AccessControlListEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing Access Control List entry.
    /// </summary>
    /// <param name="entry">The Access Control List entry to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(AccessControlListEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes an Access Control List entry (soft delete).
    /// </summary>
    /// <param name="entry">The Access Control List entry to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(AccessControlListEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes all Access Control List entries for a specific resource (soft delete).
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteByResourceAsync(
        Guid tenantId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Saves all pending changes to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
