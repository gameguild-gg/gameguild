using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Backup code MFA service.
///     Handles generation, hashing, and verification of single-use backup codes.
/// </summary>
public sealed class BackupCodeMfaService(
    ILogger<BackupCodeMfaService> logger,
    IUserMfaConfigurationRepository mfaConfigRepository,
    IMfaAttemptTrackingService attemptTrackingService) : IBackupCodeMfaService
{
    private const int BackupCodesCount = 10;

    /// <summary>
    ///     Generates backup codes for account recovery.
    /// </summary>
    public async Task<string[]> GenerateBackupCodesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Generating backup codes for user: {UserId}", userId);

        try
        {
            var mfaConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);

            if (mfaConfig is not { IsEnabled: true })
            {
                logger.LogWarning("MFA not enabled for user: {UserId}", userId);

                throw new InvalidOperationException("MFA must be enabled to generate backup codes");
            }

            // Generate backup codes (8 characters, alphanumeric)
            var backupCodes = new List<string>();

            for (var i = 0; i < BackupCodesCount; i++) { backupCodes.Add(GenerateBackupCode()); }

            // Hash backup codes before storing (like passwords)
            var hashedCodes = new List<string>();

            foreach (var code in backupCodes)
            {
                var hashedCode = await HashBackupCodeAsync(code, cancellationToken).ConfigureAwait(false);
                hashedCodes.Add(hashedCode);
            }

            // Store hashed codes
            mfaConfig.BackupCodes = string.Join(",", hashedCodes);
            mfaConfig.UpdatedAt = DateTime.UtcNow;

            await mfaConfigRepository.UpdateAsync(mfaConfig, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Backup codes generated for user: {UserId}", userId);

            // Return plain-text codes to user (only shown once)
            return backupCodes.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating backup codes for user: {UserId}", userId);

            throw;
        }
    }

    /// <summary>
    ///     Verifies a backup code and invalidates it (single-use).
    /// </summary>
    public async Task<bool> VerifyBackupCodeAsync(Guid userId, string backupCode, string? deviceId = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Verifying backup code for user: {UserId}", userId);

        try
        {
            var mfaConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);

            if (mfaConfig == null || string.IsNullOrEmpty(mfaConfig.BackupCodes))
            {
                logger.LogWarning("No backup codes found for user: {UserId}", userId);
                await attemptTrackingService.RecordMfaAttemptAsync(userId, MfaMethod.BackupCode, false, "No backup codes", deviceId, cancellationToken).ConfigureAwait(false);

                return false;
            }

            // Check lockout
            if (attemptTrackingService.IsLockedOut(mfaConfig))
            {
                logger.LogWarning("User is locked out due to failed MFA attempts: {UserId}", userId);
                await attemptTrackingService.RecordMfaAttemptAsync(userId, MfaMethod.BackupCode, false, "Account locked", deviceId, cancellationToken).ConfigureAwait(false);

                return false;
            }

            // Get all backup codes
            var hashedCodes = mfaConfig.BackupCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            // Check if code matches any stored hashed code
            var codeFound = false;
            string? matchedHashedCode = null;

            foreach (var hashedCode in hashedCodes)
            {
                if (await VerifyBackupCodeHashAsync(backupCode, hashedCode, cancellationToken))
                {
                    codeFound = true;
                    matchedHashedCode = hashedCode;

                    break;
                }
            }

            if (codeFound && matchedHashedCode != null)
            {
                // Remove used code
                hashedCodes.Remove(matchedHashedCode);
                mfaConfig.BackupCodes = string.Join(",", hashedCodes);
                mfaConfig.UpdatedAt = DateTime.UtcNow;

                // Reset failed attempts
                mfaConfig.FailedAttempts = 0;

                await mfaConfigRepository.UpdateAsync(mfaConfig, cancellationToken).ConfigureAwait(false);

                await attemptTrackingService.RecordMfaAttemptAsync(userId, MfaMethod.BackupCode, true, null, deviceId, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Backup code verification successful for user: {UserId}, Remaining codes: {RemainingCodes}", userId, hashedCodes.Count);

                return true;
            }

            // Increment failed attempts
            mfaConfig.FailedAttempts++;
            await mfaConfigRepository.UpdateAsync(mfaConfig, cancellationToken).ConfigureAwait(false);

            await attemptTrackingService.RecordMfaAttemptAsync(userId, MfaMethod.BackupCode, false, "Invalid code", deviceId, cancellationToken).ConfigureAwait(false);

            logger.LogWarning("Invalid backup code for user: {UserId}, Failed attempts: {FailedAttempts}", userId, mfaConfig.FailedAttempts);

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying backup code for user: {UserId}", userId);
            await attemptTrackingService.RecordMfaAttemptAsync(userId, MfaMethod.BackupCode, false, "System error", deviceId, cancellationToken).ConfigureAwait(false);

            return false;
        }
    }

    /// <summary>
    ///     Generates a random 8-character alphanumeric backup code.
    /// </summary>
    public string GenerateBackupCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Excludes similar-looking characters
        var random = new Random();
        var code = new char[8];

        for (var i = 0; i < code.Length; i++) { code[i] = chars[random.Next(chars.Length)]; }

        return new string(code);
    }

    /// <summary>
    ///     Hashes a backup code (similar to password hashing).
    /// </summary>
    public Task<string> HashBackupCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        // Use simple SHA256 for backup codes (not as critical as passwords)
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));

        return Task.FromResult(Convert.ToBase64String(hashBytes));
    }

    /// <summary>
    ///     Verifies a backup code against its hash.
    /// </summary>
    private async Task<bool> VerifyBackupCodeHashAsync(string code, string hash, CancellationToken cancellationToken)
    {
        var computedHash = await HashBackupCodeAsync(code, cancellationToken).ConfigureAwait(false);

        return computedHash == hash;
    }

    /// <summary>
    ///     Stores hashed backup codes for a user during MFA setup (before MFA is fully enabled).
    /// </summary>
    public async Task StoreBackupCodesForSetupAsync(Guid userId, IReadOnlyList<string> plainTextCodes, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Storing backup codes during MFA setup for user: {UserId}", userId);

        var mfaConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (mfaConfig == null) { return; }

        var hashedCodes = new List<string>();
        foreach (var code in plainTextCodes)
        {
            var hashedCode = await HashBackupCodeAsync(code, cancellationToken).ConfigureAwait(false);
            hashedCodes.Add(hashedCode);
        }

        mfaConfig.BackupCodes = string.Join(",", hashedCodes);
        mfaConfig.UpdatedAt = DateTime.UtcNow;
        await mfaConfigRepository.UpdateAsync(mfaConfig, cancellationToken).ConfigureAwait(false);
    }
}
