namespace GameGuild.Modules.Credentials;

/// <summary>
/// Repository interface for credential data access operations
/// Follows hexagonal architecture principles as a port (interface)
/// </summary>
public interface ICredentialRepository : IRepository<Credential>
{
    /// <summary>
    /// Get all credentials for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of credentials for the user</returns>
    Task<IEnumerable<Credential>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific credential by user ID and type
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="type">Credential type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Credential or null if not found</returns>
    Task<Credential?> GetByUserIdAndTypeAsync(Guid userId, string type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get credential by ID with user included
    /// </summary>
    /// <param name="id">Credential ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Credential with user or null if not found</returns>
    Task<Credential?> GetByIdWithUserAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all credentials including soft-deleted ones
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all credentials</returns>
    Task<IEnumerable<Credential>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get only soft-deleted credentials
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of soft-deleted credentials</returns>
    Task<IEnumerable<Credential>> GetDeletedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a credential as used by updating LastUsedAt
    /// </summary>
    /// <param name="id">Credential ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> MarkAsUsedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activate a credential
    /// </summary>
    /// <param name="id">Credential ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if activated successfully</returns>
    Task<bool> ActivateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate a credential
    /// </summary>
    /// <param name="id">Credential ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deactivated successfully</returns>
    Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
