namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service interface for managing service accounts and client credentials authentication.
/// </summary>
public interface IServiceAccountService
{
    /// <summary>
    ///     Creates a new service account and returns the plaintext client secret (only shown once).
    /// </summary>
    /// <param name="name">Human-readable name for the service account.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="tenantId">Tenant ID (null for global service accounts).</param>
    /// <param name="scopes">Comma-separated list of scopes.</param>
    /// <param name="createdBy">User ID or "system" who created this.</param>
    /// <param name="allowedIpAddresses">Optional comma-separated allowed IP addresses.</param>
    /// <param name="expiresAt">Optional expiration date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created service account and the plaintext client secret.</returns>
    Task<(ServiceAccount Account, string ClientSecret)> CreateServiceAccountAsync(
        string name,
        string? description,
        Guid? tenantId,
        string scopes,
        string createdBy,
        string? allowedIpAddresses = null,
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Authenticates a service account using client credentials.
    /// </summary>
    /// <param name="clientId">The client ID.</param>
    /// <param name="clientSecret">The client secret.</param>
    /// <param name="ipAddress">The IP address of the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticated service account, or null if authentication failed.</returns>
    Task<ServiceAccount?> AuthenticateAsync(
        string clientId,
        string clientSecret,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Rotates the client secret for a service account.
    /// </summary>
    /// <returns>The new plaintext client secret (only shown once).</returns>
    Task<string> RotateSecretAsync(Guid serviceAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Unlocks a locked service account.
    /// </summary>
    Task UnlockAsync(Guid serviceAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deactivates a service account.
    /// </summary>
    Task DeactivateAsync(Guid serviceAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Reactivates a deactivated service account.
    /// </summary>
    Task ReactivateAsync(Guid serviceAccountId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates the scopes for a service account.
    /// </summary>
    Task UpdateScopesAsync(Guid serviceAccountId, string scopes, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a service account by ID.
    /// </summary>
    Task<ServiceAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all service accounts for a tenant.
    /// </summary>
    Task<IReadOnlyList<ServiceAccount>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
