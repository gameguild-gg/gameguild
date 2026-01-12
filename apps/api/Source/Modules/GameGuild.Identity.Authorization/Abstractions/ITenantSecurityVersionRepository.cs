namespace GameGuild.Identity.Authorization;

/// <summary>
///     Repository interface for managing tenant security versions.
/// </summary>
public interface ITenantSecurityVersionRepository
{
    /// <summary>
    ///     Gets the security version record for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The security version entity, or null if not found.</returns>
    Task<TenantSecurityVersion?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets or creates a security version record for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The security version entity (created if it didn't exist).</returns>
    Task<TenantSecurityVersion> GetOrCreateAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Increments the security version for a tenant and saves.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="reason">Optional reason for the increment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new version number.</returns>
    Task<long> IncrementVersionAsync(Guid tenantId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds a new security version record.
    /// </summary>
    /// <param name="version">The security version entity to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(TenantSecurityVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing security version record.
    /// </summary>
    /// <param name="version">The security version entity to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(TenantSecurityVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Saves pending changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
