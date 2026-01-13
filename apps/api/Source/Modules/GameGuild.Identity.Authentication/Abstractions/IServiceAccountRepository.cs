namespace GameGuild.Identity.Authentication;

/// <summary>
///     Repository interface for service account persistence.
/// </summary>
public interface IServiceAccountRepository
{
    /// <summary>
    ///     Gets a service account by its unique ID.
    /// </summary>
    Task<ServiceAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a service account by its client ID.
    /// </summary>
    Task<ServiceAccount?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all service accounts for a tenant.
    /// </summary>
    Task<IReadOnlyList<ServiceAccount>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all global (non-tenant-specific) service accounts.
    /// </summary>
    Task<IReadOnlyList<ServiceAccount>> GetGlobalServiceAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates a new service account.
    /// </summary>
    Task<ServiceAccount> CreateAsync(ServiceAccount serviceAccount, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing service account.
    /// </summary>
    Task<ServiceAccount> UpdateAsync(ServiceAccount serviceAccount, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a service account.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a client ID already exists.
    /// </summary>
    Task<bool> ClientIdExistsAsync(string clientId, CancellationToken cancellationToken = default);
}
