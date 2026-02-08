namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for MFA attempt tracking, lockout management, and configuration queries.
///     Handles attempt recording, lockout checks, and failed-attempt resets.
/// </summary>
public interface IMfaAttemptTrackingService
{
    /// <summary>
    ///     Gets the MFA configuration for a user including enabled methods and backup codes remaining.
    /// </summary>
    Task<MfaConfigurationResponse> GetMfaConfigurationAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets MFA status for a user.
    /// </summary>
    Task<bool> GetMfaStatusAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if MFA lockout is active for a user.
    /// </summary>
    Task<bool> IsUserLockedOutAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Disables MFA for a user (requires password confirmation in caller).
    /// </summary>
    Task<bool> DisableMfaAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets MFA attempt history for analysis.
    /// </summary>
    Task<IEnumerable<MfaAttempt>> GetMfaAttemptsAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resets failed MFA attempts (admin function).
    /// </summary>
    Task<bool> ResetFailedAttemptsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Records MFA attempt for auditing and analytics.
    /// </summary>
    Task RecordMfaAttemptAsync(Guid userId, MfaMethod method, bool success, string? failureReason, string? deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if user is currently locked out due to failed MFA attempts.
    /// </summary>
    bool IsLockedOut(UserMfaConfiguration mfaConfig);

    /// <summary>
    ///     Checks if MFA is required by policy for a user (e.g. admin/elevated roles, tenant-level policy).
    ///     This is distinct from whether the user has MFA enabled.
    /// </summary>
    Task<bool> IsMfaRequiredByPolicyAsync(Guid userId, CancellationToken cancellationToken = default);
}
