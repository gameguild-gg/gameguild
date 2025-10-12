namespace GameGuild.Modules.Authentication;

/// <summary>
/// Repository interface for user MFA configuration data access operations
/// </summary>
public interface IUserMfaConfigurationRepository
{
    /// <summary>
    /// Get MFA configuration by ID
    /// </summary>
    /// <param name="id">The configuration ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The MFA configuration or null if not found</returns>
    Task<UserMfaConfiguration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get MFA configuration by user ID
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The MFA configuration or null if not found</returns>
    Task<UserMfaConfiguration?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all users with MFA enabled
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of MFA configurations for enabled users</returns>
    Task<IReadOnlyList<UserMfaConfiguration>> GetEnabledConfigurationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if MFA is enabled for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if MFA is enabled and setup is complete</returns>
    Task<bool> IsMfaEnabledAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new MFA configuration
    /// </summary>
    /// <param name="configuration">The MFA configuration to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created MFA configuration</returns>
    Task<UserMfaConfiguration> CreateAsync(UserMfaConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing MFA configuration
    /// </summary>
    /// <param name="configuration">The MFA configuration to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated MFA configuration</returns>
    Task<UserMfaConfiguration> UpdateAsync(UserMfaConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enable MFA for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if enabled, false if not found</returns>
    Task<bool> EnableMfaAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disable MFA for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if disabled, false if not found</returns>
    Task<bool> DisableMfaAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update failed attempts count
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="increment">Whether to increment (true) or reset (false) the count</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated failed attempts count</returns>
    Task<int> UpdateFailedAttemptsAsync(Guid userId, bool increment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lock out a user due to failed MFA attempts
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="lockoutUntil">When the lockout expires</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if locked out, false if not found</returns>
    Task<bool> LockoutUserAsync(Guid userId, DateTime lockoutUntil, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear lockout for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cleared, false if not found</returns>
    Task<bool> ClearLockoutAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an MFA configuration
    /// </summary>
    /// <param name="id">The configuration ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete MFA configuration by user ID
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
