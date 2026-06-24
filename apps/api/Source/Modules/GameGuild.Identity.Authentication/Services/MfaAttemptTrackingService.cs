using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     MFA attempt tracking and lockout management service.
///     Handles configuration queries, attempt recording, lockout checks, and MFA lifecycle management.
/// </summary>
public sealed class MfaAttemptTrackingService(
    ILogger<MfaAttemptTrackingService> logger,
    IUserMfaConfigurationRepository mfaConfigRepository,
    IMfaAttemptRepository mfaAttemptRepository,
    IHttpContextAccessor httpContextAccessor) : IMfaAttemptTrackingService
{
    private const int MaxFailedAttempts = 5;

    /// <summary>
    ///     Gets the MFA configuration for a user including enabled methods and backup codes remaining.
    /// </summary>
    public async Task<MfaConfigurationResponse> GetMfaConfigurationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting MFA configuration for user: {UserId}", userId);

        try
        {
            var mfaConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);

            if (mfaConfig == null || !mfaConfig.IsEnabled)
            {
                return new MfaConfigurationResponse
                {
                    IsEnabled = false,
                    EnabledMethods = [],
                    EnabledAt = null,
                    BackupCodesRemaining = 0
                };
            }

            // Count remaining backup codes
            var backupCodesRemaining = 0;
            if (!string.IsNullOrEmpty(mfaConfig.BackupCodes))
            {
                var codes = mfaConfig.BackupCodes.Split(',', StringSplitOptions.RemoveEmptyEntries);
                backupCodesRemaining = codes.Length;
            }

            // Build list of enabled methods
            var enabledMethods = new List<string>();
            if (!string.IsNullOrEmpty(mfaConfig.TotpSecretKey))
            {
                enabledMethods.Add(MfaMethod.Totp.ToString());
            }
            if (backupCodesRemaining > 0)
            {
                enabledMethods.Add(MfaMethod.BackupCode.ToString());
            }

            return new MfaConfigurationResponse
            {
                IsEnabled = mfaConfig.IsEnabled,
                EnabledMethods = enabledMethods.ToArray(),
                EnabledAt = mfaConfig.EnabledAt,
                BackupCodesRemaining = backupCodesRemaining
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting MFA configuration for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    ///     Gets MFA status for a user.
    /// </summary>
    public async Task<bool> GetMfaStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var mfaConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);

            return mfaConfig?.IsEnabled == true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting MFA status for user: {UserId}", userId);

            return false;
        }
    }

    /// <summary>
    ///     Checks if MFA lockout is active for a user.
    /// </summary>
    public async Task<bool> IsUserLockedOutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var mfaConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);

            return mfaConfig != null && IsLockedOut(mfaConfig);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking MFA lockout for user: {UserId}", userId);

            return false;
        }
    }

    /// <summary>
    ///     Disables MFA for a user (requires password confirmation in caller).
    /// </summary>
    public async Task<bool> DisableMfaAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Disabling MFA for user: {UserId}", userId);

        try
        {
            var mfaConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);

            if (mfaConfig is not { IsEnabled: true })
            {
                logger.LogWarning("MFA not enabled for user: {UserId}", userId);

                return false;
            }

            // Soft delete or disable
            mfaConfig.IsEnabled = false;
            mfaConfig.UpdatedAt = SystemClock.UtcNow;
            mfaConfig.TotpSecretKey = null;
            mfaConfig.BackupCodes = null;
            mfaConfig.FailedAttempts = 0;

            await mfaConfigRepository.UpdateAsync(mfaConfig, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("MFA disabled for user: {UserId}", userId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error disabling MFA for user: {UserId}", userId);

            return false;
        }
    }

    /// <summary>
    ///     Gets MFA attempt history for analysis.
    /// </summary>
    public async Task<IEnumerable<MfaAttempt>> GetMfaAttemptsAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default)
    {
        try { return await mfaAttemptRepository.GetByUserIdAsync(userId, limit, cancellationToken).ConfigureAwait(false); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting MFA attempts for user: {UserId}", userId);

            return [];
        }
    }

    /// <summary>
    ///     Resets failed MFA attempts (admin function).
    /// </summary>
    public async Task<bool> ResetFailedAttemptsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Resetting failed MFA attempts for user: {UserId}", userId);

        try
        {
            var mfaConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);

            if (mfaConfig == null)
            {
                logger.LogWarning("No MFA configuration found for user: {UserId}", userId);

                return false;
            }

            mfaConfig.FailedAttempts = 0;
            mfaConfig.UpdatedAt = SystemClock.UtcNow;

            await mfaConfigRepository.UpdateAsync(mfaConfig, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Failed MFA attempts reset for user: {UserId}", userId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting failed MFA attempts for user: {UserId}", userId);

            return false;
        }
    }

    /// <summary>
    ///     Records MFA attempt for auditing and analytics.
    /// </summary>
    public async Task RecordMfaAttemptAsync(Guid userId, MfaMethod method, bool success, string? failureReason, string? deviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var attempt = new MfaAttempt
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Method = method,
                IsSuccessful = success,
                FailureReason = failureReason,
                DeviceFingerprint = deviceId,
                IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0",
                UserAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? "Unknown",
                AttemptedAt = SystemClock.UtcNow,
                ProcessingTimeMs = 0
            };

            await mfaAttemptRepository.CreateAsync(attempt, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recording MFA attempt for user: {UserId}", userId);
            // Don't throw - logging failure shouldn't break authentication
        }
    }

    /// <summary>
    ///     Checks if user is currently locked out due to failed MFA attempts.
    /// </summary>
    public bool IsLockedOut(UserMfaConfiguration mfaConfig)
    {
        if (mfaConfig.FailedAttempts < MaxFailedAttempts) { return false; }

        if (!mfaConfig.LockedOutUntil.HasValue) { return false; }

        return SystemClock.UtcNow < mfaConfig.LockedOutUntil.Value;
    }

    /// <summary>
    ///     Checks if MFA is required by policy for a user.
    ///     Evaluates admin/elevated role requirements and tenant-level MFA policies.
    /// </summary>
    public Task<bool> IsMfaRequiredByPolicyAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Check if the current user has an elevated role that should require MFA.
        // This inspects the ClaimsPrincipal from HttpContext (populated by JWT middleware).
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.IsInRole("Admin") == true || user?.IsInRole("SuperAdmin") == true)
        {
            return Task.FromResult(true);
        }

        // NOTE: Tenant-level MFA policies should be checked here once
        // tenant configuration includes an MFA requirement flag.
        return Task.FromResult(false);
    }
}
