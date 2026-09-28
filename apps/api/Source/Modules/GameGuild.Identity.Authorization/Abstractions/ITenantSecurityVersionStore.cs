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

    /// <summary>Reads one tenant's version and the shared global version together.</summary>
    Task<(long TenantVersion, long GlobalVersion)> GetTenantAndGlobalVersionsAsync(Guid tenantId) =>
        GetTenantAndGlobalVersionsAsync(tenantId, CancellationToken.None);

    async Task<(long TenantVersion, long GlobalVersion)> GetTenantAndGlobalVersionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var tenantVersionTask = GetVersionAsync(tenantId.ToString(), cancellationToken);
        var globalVersionTask = tenantId == Guid.Empty
            ? tenantVersionTask
            : GetVersionAsync(Guid.Empty.ToString(), cancellationToken);
        await Task.WhenAll(tenantVersionTask, globalVersionTask).ConfigureAwait(false);
        return (await tenantVersionTask.ConfigureAwait(false), await globalVersionTask.ConfigureAwait(false));
    }

    /// <summary>
    ///     Increments the security version for a tenant (triggers cache invalidation).
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new version number.</returns>
    Task<long> IncrementVersionAsync(string tenantId, CancellationToken cancellationToken = default);
}
