using GameGuild.Authentication.Entities;
using GameGuild.Authentication.Enums;

namespace GameGuild.Authentication.Abstractions;

/// <summary>
///     Repository for managing user MFA configurations.
///     Stores MFA settings, secrets, backup codes, and preferences.
/// </summary>
public interface IUserMfaConfigurationRepository
{
    /// <summary>
    ///     Creates a new MFA configuration for a user.
    /// </summary>
    Task<UserMfaConfiguration> CreateAsync(UserMfaConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets MFA configuration by user ID.
    /// </summary>
    Task<UserMfaConfiguration?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing MFA configuration.
    /// </summary>
    Task<UserMfaConfiguration> UpdateAsync(UserMfaConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes MFA configuration for a user (when disabling MFA).
    /// </summary>
    Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if MFA is enabled for a user.
    /// </summary>
    Task<bool> IsMfaEnabledAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the preferred MFA method for a user.
    /// </summary>
    Task<MfaMethod?> GetPreferredMethodAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Increments failed MFA attempts counter.
    /// </summary>
    Task IncrementFailedAttemptsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resets failed MFA attempts counter to zero.
    /// </summary>
    Task ResetFailedAttemptsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets MFA lockout until a specific time.
    /// </summary>
    Task SetLockoutAsync(Guid userId, DateTime lockoutUntil, CancellationToken cancellationToken = default);
}
