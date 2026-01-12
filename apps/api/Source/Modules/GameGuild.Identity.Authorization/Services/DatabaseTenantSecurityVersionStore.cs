namespace GameGuild.Identity.Authorization;

/// <summary>
///     Database-backed implementation of tenant security version store.
///     Uses the ITenantSecurityVersionRepository for persistence.
/// </summary>
public sealed class DatabaseTenantSecurityVersionStore(ITenantSecurityVersionRepository repository) : ITenantSecurityVersionStore
{
    /// <inheritdoc />
    public async Task<long> GetVersionAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return 0;

        var version = await repository.GetByTenantIdAsync(tenantGuid, cancellationToken).ConfigureAwait(false);
        return version?.SecurityVersion ?? 0;
    }

    /// <inheritdoc />
    public async Task<long> IncrementVersionAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(tenantId, out var tenantGuid))
            return 0;

        return await repository.IncrementVersionAsync(tenantGuid, reason: null, cancellationToken).ConfigureAwait(false);
    }
}
