namespace GameGuild.Identity.Authorization;

/// <summary>
///     Stores security version numbers for cache invalidation.
/// </summary>
public interface ITenantSecurityVersionStore
{
    /// <summary>
    ///     Gets the current security version for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current version number.</returns>
    Task<long> GetVersionAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Increments the security version for a tenant (triggers cache invalidation).
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new version number.</returns>
    Task<long> IncrementVersionAsync(string tenantId, CancellationToken cancellationToken = default);
}
