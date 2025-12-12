using System.Collections;
using System.Security.Cryptography;
using System.Text;
using GameGuild.Authentication.Abstractions;
using GameGuild.Authentication.Entities;
using GameGuild.Authentication.Enums;
using GameGuild.Authentication.Models.Responses;
using Microsoft.Extensions.Logging;
using UserMfaConfiguration = GameGuild.Authentication.Entities.UserMfaConfiguration;

namespace GameGuild.Authentication.Services;

/// <summary>
///     Multi-Factor Authentication (MFA) service supporting TOTP, backup codes, and SMS.
///     Handles MFA setup, verification, lockout tracking, and recovery.
/// </summary>
public sealed class MfaService(ILogger<MfaService> logger, IUserMfaConfigurationRepository mfaConfigRepository, IMfaAttemptRepository mfaAttemptRepository, IEncryptionService encryptionService) : IMfaService
{
    private const int MaxFailedAttempts = 5;

    private const int LockoutDurationMinutes = 15;

    private const int BackupCodesCount = 10;

    private const int TotpWindow = 1; // Allow 1 step before/after current time

    /// <summary>
    ///     Gets the MFA configuration for a user including enabled methods and backup codes remaining
    /// </summary>
    public async Task<DTOs.MfaConfigurationResponse> GetMfaConfigurationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Getting MFA configuration for user: {UserId}", userId);

        try
        {
            var mfaConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken);

            if (mfaConfig == null || !mfaConfig.IsEnabled)
            {
                return new DTOs.MfaConfigurationResponse
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

            return new DTOs.MfaConfigurationResponse
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
    ///     Generates backup codes for account recovery.
    /// </summary>
    public async Task<string[ ]> GenerateBackupCodesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Generating backup codes for user: {UserId}", userId);

        try
        {
            var mfaConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken);

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
                var hashedCode = await HashBackupCodeAsync(code, cancellationToken);
                hashedCodes.Add(hashedCode);
            }

            // Store hashed codes
            mfaConfig.BackupCodes = string.Join(",", hashedCodes);
            mfaConfig.UpdatedAt = DateTime.UtcNow;

            await mfaConfigRepository.UpdateAsync(mfaConfig, cancellationToken);

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
            var mfaConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken);

            if (mfaConfig == null || string.IsNullOrEmpty(mfaConfig.BackupCodes))
            {
                logger.LogWarning("No backup codes found for user: {UserId}", userId);
                await RecordMfaAttemptAsync(userId, MfaMethod.BackupCode, false, "No backup codes", deviceId, cancellationToken);

                return false;
            }

            // Check lockout
            if (IsLockedOut(mfaConfig))
            {
                logger.LogWarning("User is locked out due to failed MFA attempts: {UserId}", userId);
                await RecordMfaAttemptAsync(userId, MfaMethod.BackupCode, false, "Account locked", deviceId, cancellationToken);

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

                await mfaConfigRepository.UpdateAsync(mfaConfig, cancellationToken);

                await RecordMfaAttemptAsync(userId, MfaMethod.BackupCode, true, null, deviceId, cancellationToken);

                logger.LogInformation("Backup code verification successful for user: {UserId}, Remaining codes: {RemainingCodes}", userId, hashedCodes.Count);

                return true;
            }

            // Increment failed attempts
            mfaConfig.FailedAttempts++;
            await mfaConfigRepository.UpdateAsync(mfaConfig, cancellationToken);

            await RecordMfaAttemptAsync(userId, MfaMethod.BackupCode, false, "Invalid code", deviceId, cancellationToken);

            logger.LogWarning("Invalid backup code for user: {UserId}, Failed attempts: {FailedAttempts}", userId, mfaConfig.FailedAttempts);

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying backup code for user: {UserId}", userId);
            await RecordMfaAttemptAsync(userId, MfaMethod.BackupCode, false, "System error", deviceId, cancellationToken);

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
            var mfaConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken);

            return mfaConfig != null && IsLockedOut(mfaConfig);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking MFA lockout for user: {UserId}", userId);

            return false;
        }
    }

    /// <summary>
    ///     Sets up TOTP-based MFA for a user. Returns QR code URI and secret key.
    /// </summary>
    public async Task<(string QrCodeUri, string SecretKey)> SetupTotpAsync(Guid userId, string userEmail, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Setting up TOTP MFA for user: {UserId}", userId);

        try
        {
            // Check if MFA already enabled
            var existingConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken);

            if (existingConfig?.IsEnabled == true)
            {
                logger.LogWarning("MFA already enabled for user: {UserId}", userId);

                throw new InvalidOperationException("MFA is already enabled for this user");
            }

            // Generate secret key (Base32 encoded, 160 bits = 32 characters)
            var secretKey = GenerateBase32Secret();

            // Encrypt secret key before storing
            var encryptedSecret = encryptionService.Encrypt(secretKey);

            // Create or update MFA configuration
            var mfaConfig = existingConfig ?? new UserMfaConfiguration { Id = Guid.NewGuid(), UserId = userId, CreatedAt = DateTime.UtcNow };

            mfaConfig.PreferredMethod = MfaMethod.Totp;
            mfaConfig.TotpSecretKey = encryptedSecret;
            mfaConfig.IsEnabled = false; // Enabled after first successful verification
            mfaConfig.UpdatedAt = DateTime.UtcNow;

            if (existingConfig == null) { await mfaConfigRepository.CreateAsync(mfaConfig, cancellationToken); }
            else { await mfaConfigRepository.UpdateAsync(mfaConfig, cancellationToken); }

            // Generate QR code URI (otpauth://totp/...)
            var qrCodeUri = GenerateTotpUri(userEmail, secretKey);

            logger.LogInformation("TOTP setup successful for user: {UserId}", userId);

            return (qrCodeUri, secretKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting up TOTP for user: {UserId}", userId);

            throw;
        }
    }

    /// <summary>
    ///     Verifies TOTP code and enables MFA if first-time setup.
    /// </summary>
    public async Task<bool> VerifyTotpAsync(Guid userId, string totpCode, string? deviceId = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Verifying TOTP code for user: {UserId}", userId);

        try
        {
            // Get MFA configuration
            var mfaConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken);

            if (mfaConfig == null || string.IsNullOrEmpty(mfaConfig.TotpSecretKey))
            {
                logger.LogWarning("No TOTP configuration found for user: {UserId}", userId);
                await RecordMfaAttemptAsync(userId, MfaMethod.Totp, false, "No TOTP configuration", deviceId, cancellationToken);

                return false;
            }

            // Check lockout
            if (IsLockedOut(mfaConfig))
            {
                logger.LogWarning("User is locked out due to failed MFA attempts: {UserId}", userId);
                await RecordMfaAttemptAsync(userId, MfaMethod.Totp, false, "Account locked", deviceId, cancellationToken);

                return false;
            }

            // Decrypt secret key
            var secretKey = encryptionService.Decrypt(mfaConfig.TotpSecretKey);

            // Verify TOTP code
            var isValid = VerifyTotpCode(secretKey, totpCode, TotpWindow);

            if (isValid)
            {
                // Enable MFA if first successful verification
                if (!mfaConfig.IsEnabled)
                {
                    mfaConfig.IsEnabled = true;
                    mfaConfig.EnabledAt = DateTime.UtcNow;
                    mfaConfig.UpdatedAt = DateTime.UtcNow;
                    await mfaConfigRepository.UpdateAsync(mfaConfig, cancellationToken);

                    logger.LogInformation("MFA enabled for user: {UserId}", userId);
                }

                // Reset failed attempts
                if (mfaConfig.FailedAttempts > 0)
                {
                    mfaConfig.FailedAttempts = 0;
                    await mfaConfigRepository.UpdateAsync(mfaConfig, cancellationToken);
                }

                await RecordMfaAttemptAsync(userId, MfaMethod.Totp, true, null, deviceId, cancellationToken);

                logger.LogInformation("TOTP verification successful for user: {UserId}", userId);

                return true;
            }

            // Increment failed attempts
            mfaConfig.FailedAttempts++;
            await mfaConfigRepository.UpdateAsync(mfaConfig, cancellationToken);

            await RecordMfaAttemptAsync(userId, MfaMethod.Totp, false, "Invalid code", deviceId, cancellationToken);

            logger.LogWarning("Invalid TOTP code for user: {UserId}, Failed attempts: {FailedAttempts}", userId, mfaConfig.FailedAttempts);

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying TOTP for user: {UserId}", userId);
            await RecordMfaAttemptAsync(userId, MfaMethod.Totp, false, "System error", deviceId, cancellationToken);

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
            var mfaConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken);

            if (mfaConfig is not { IsEnabled: true })
            {
                logger.LogWarning("MFA not enabled for user: {UserId}", userId);

                return false;
            }

            // Soft delete or disable
            mfaConfig.IsEnabled = false;
            mfaConfig.UpdatedAt = DateTime.UtcNow;
            mfaConfig.TotpSecretKey = null;
            mfaConfig.BackupCodes = null;
            mfaConfig.FailedAttempts = 0;

            await mfaConfigRepository.UpdateAsync(mfaConfig, cancellationToken);

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
    ///     Gets MFA status for a user.
    /// </summary>
    public async Task<bool> GetMfaStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var mfaConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken);

            return mfaConfig?.IsEnabled == true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting MFA status for user: {UserId}", userId);

            return false;
        }
    }

    /// <summary>
    ///     Gets detailed MFA configuration entity for a user (internal use).
    /// </summary>
    private async Task<UserMfaConfiguration?> GetMfaConfigurationEntityAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try { return await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting MFA configuration for user: {UserId}", userId);

            return null;
        }
    }

    /// <summary>
    ///     Gets MFA attempt history for analysis.
    /// </summary>
    public async Task<IEnumerable<MfaAttempt>> GetMfaAttemptsAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default)
    {
        try { return await mfaAttemptRepository.GetByUserIdAsync(userId, limit, cancellationToken); }
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
            var mfaConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken);

            if (mfaConfig == null)
            {
                logger.LogWarning("No MFA configuration found for user: {UserId}", userId);

                return false;
            }

            mfaConfig.FailedAttempts = 0;
            mfaConfig.UpdatedAt = DateTime.UtcNow;

            await mfaConfigRepository.UpdateAsync(mfaConfig, cancellationToken);

            logger.LogInformation("Failed MFA attempts reset for user: {UserId}", userId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting failed MFA attempts for user: {UserId}", userId);

            return false;
        }
    }

    #region Private Helper Methods

    /// <summary>
    ///     Checks if user is currently locked out due to failed MFA attempts.
    /// </summary>
    private bool IsLockedOut(UserMfaConfiguration mfaConfig)
    {
        if (mfaConfig.FailedAttempts < MaxFailedAttempts) { return false; }

        if (!mfaConfig.LockedOutUntil.HasValue) { return false; }

        return DateTime.UtcNow < mfaConfig.LockedOutUntil.Value;
    }

    /// <summary>
    ///     Generates a Base32-encoded secret key for TOTP (160 bits = 32 characters).
    /// </summary>
    private string GenerateBase32Secret()
    {
        const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var random = new Random();
        var secret = new char[32];

        for (var i = 0; i < secret.Length; i++) { secret[i] = base32Chars[random.Next(base32Chars.Length)]; }

        return new string(secret);
    }

    /// <summary>
    ///     Generates TOTP URI for QR code (otpauth://totp/...).
    /// </summary>
    private string GenerateTotpUri(string userEmail, string secretKey)
    {
        var issuer = "GameGuild";
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(userEmail);

        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secretKey}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
    }

    /// <summary>
    ///     Verifies TOTP code using time-based algorithm (RFC 6238).
    /// </summary>
    private bool VerifyTotpCode(string secretKey, string totpCode, int window)
    {
        var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timeStep = currentTimestamp / 30; // 30-second time step

        // Check current time step and window steps before/after
        for (var i = -window; i <= window; i++)
        {
            var testStep = timeStep + i;
            var expectedCode = GenerateTotpCode(secretKey, testStep);

            if (expectedCode == totpCode) { return true; }
        }

        return false;
    }

    /// <summary>
    ///     Generates TOTP code for a given time step.
    /// </summary>
    private string GenerateTotpCode(string secretKey, long timeStep)
    {
        // Convert Base32 secret to bytes
        var keyBytes = Base32Decode(secretKey);

        // Convert time step to bytes
        var timeBytes = BitConverter.GetBytes(timeStep);

        if (BitConverter.IsLittleEndian) { Array.Reverse(timeBytes); }

        // HMAC-SHA1
        using var hmac = new HMACSHA1(keyBytes);
        var hash = hmac.ComputeHash(timeBytes);

        // Dynamic truncation
        var offset = hash[^1] & 0x0F;
        var truncatedHash = (hash[offset] & 0x7F) << 24 | (hash[offset + 1] & 0xFF) << 16 | (hash[offset + 2] & 0xFF) << 8 | hash[offset + 3] & 0xFF;

        // Generate 6-digit code
        var code = truncatedHash % 1000000;

        return code.ToString("D6");
    }

    /// <summary>
    ///     Decodes Base32 string to bytes.
    /// </summary>
    private byte[ ] Base32Decode(string base32)
    {
        const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        base32 = base32.ToUpperInvariant().Replace(" ", "").Replace("-", "");

        var bits = new BitArray(base32.Length * 5);
        var bitIndex = 0;

        foreach (var c in base32)
        {
            var value = base32Chars.IndexOf(c);

            if (value < 0) { throw new ArgumentException("Invalid Base32 character"); }

            for (var i = 4; i >= 0; i--) { bits[bitIndex++] = (value >> i & 1) == 1; }
        }

        var bytes = new byte[bitIndex / 8];

        for (var i = 0; i < bytes.Length; i++)
        {
            for (var j = 0; j < 8; j++)
            {
                if (bits[i * 8 + j]) { bytes[i] |= (byte) (1 << 7 - j); }
            }
        }

        return bytes;
    }

    /// <summary>
    ///     Generates a random 8-character alphanumeric backup code.
    /// </summary>
    private string GenerateBackupCode()
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
    private async Task<string> HashBackupCodeAsync(string code, CancellationToken cancellationToken)
    {
        // Use simple SHA256 for backup codes (not as critical as passwords)
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));

        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    ///     Verifies a backup code against its hash.
    /// </summary>
    private async Task<bool> VerifyBackupCodeHashAsync(string code, string hash, CancellationToken cancellationToken)
    {
        var computedHash = await HashBackupCodeAsync(code, cancellationToken);

        return computedHash == hash;
    }

    /// <summary>
    ///     Records MFA attempt for auditing and analytics.
    /// </summary>
    private async Task RecordMfaAttemptAsync(Guid userId, MfaMethod method, bool success, string? failureReason, string? deviceId, CancellationToken cancellationToken)
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
                IpAddress = "0.0.0.0", // TODO: Get from context
                UserAgent = "Unknown", // TODO: Get from context
                AttemptedAt = DateTime.UtcNow,
                ProcessingTimeMs = 0
            };

            await mfaAttemptRepository.CreateAsync(attempt, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recording MFA attempt for user: {UserId}", userId);
            // Don't throw - logging failure shouldn't break authentication
        }
    }

    #endregion

    #region Interface Implementation Methods

    /// <summary>
    ///     Initiates MFA setup for a user by generating a secret and QR code URI.
    /// </summary>
    public async Task<MfaSetupResult> InitiateMfaSetupAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use a temporary email or get from user context
            var userEmail = $"user_{userId}@game-guild.com"; // TODO: Get actual user email
            (var qrCodeUri, var secretKey) = await SetupTotpAsync(userId, userEmail, cancellationToken);

            // Generate backup codes during setup
            var backupCodes = new List<string>();
            for (var i = 0; i < BackupCodesCount; i++)
            {
                backupCodes.Add(GenerateBackupCode());
            }

            // Store hashed backup codes
            var mfaConfig = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken);
            if (mfaConfig != null)
            {
                var hashedCodes = new List<string>();
                foreach (var code in backupCodes)
                {
                    var hashedCode = await HashBackupCodeAsync(code, cancellationToken);
                    hashedCodes.Add(hashedCode);
                }
                
                mfaConfig.BackupCodes = string.Join(",", hashedCodes);
                mfaConfig.UpdatedAt = DateTime.UtcNow;
                await mfaConfigRepository.UpdateAsync(mfaConfig, cancellationToken);
            }

            return new MfaSetupResult 
            { 
                Success = true, 
                SecretKey = secretKey, 
                QrCodeUri = qrCodeUri, 
                QrCodeUrl = qrCodeUri, 
                BackupCodes = backupCodes.ToArray(),
                Message = "MFA setup initiated successfully" 
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initiate MFA setup for user: {UserId}", userId);

            return new MfaSetupResult { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    ///     Completes MFA setup by verifying the TOTP code.
    /// </summary>
    public async Task<MfaVerificationResult> CompleteMfaSetupAsync(Guid userId, string totpCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var isValid = await VerifyTotpAsync(userId, totpCode, null, cancellationToken);

            return new MfaVerificationResult { Success = isValid, Message = isValid ? "MFA setup completed successfully" : "Invalid TOTP code" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to complete MFA setup for user: {UserId}", userId);

            return new MfaVerificationResult { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    ///     Verifies an MFA code (TOTP or backup code).
    /// </summary>
    public async Task<MfaVerificationResult> VerifyMfaAsync(Guid userId, string code, MfaMethod method = MfaMethod.Totp, CancellationToken cancellationToken = default)
    {
        try
        {
            bool isValid;

            if (method == MfaMethod.Totp) { isValid = await VerifyTotpAsync(userId, code, null, cancellationToken); }
            else if (method == MfaMethod.BackupCode) { isValid = await VerifyBackupCodeAsync(userId, code, null, cancellationToken); }
            else { isValid = false; }

            return new MfaVerificationResult { Success = isValid, RequiresAdditionalVerification = false, Message = isValid ? "MFA verification successful" : "Invalid MFA code" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to verify MFA for user: {UserId}", userId);

            return new MfaVerificationResult { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    ///     Disables MFA for a user after confirming with a code.
    /// </summary>
    public async Task<bool> DisableMfaAsync(Guid userId, string confirmationCode, CancellationToken cancellationToken = default)
    {
        // Verify the confirmation code before disabling
        var verificationResult = await VerifyMfaAsync(userId, confirmationCode, MfaMethod.Totp, cancellationToken);

        if (!verificationResult.Success) { return false; }

        return await DisableMfaAsync(userId, cancellationToken);
    }

    /// <summary>
    ///     Generates a QR code image from QR code data.
    /// </summary>
    public async Task<byte[ ]> GenerateQrCodeAsync(string qrCodeData, CancellationToken cancellationToken = default)
    {
        // TODO: Implement QR code generation using a library like QRCoder or ZXing
        // For now, return empty array
        await Task.CompletedTask;
        logger.LogWarning("QR code generation not implemented yet");

        return [];
    }

    /// <summary>
    ///     Checks if MFA is enabled for a user.
    /// </summary>
    public async Task<bool> IsMfaEnabledAsync(Guid userId, CancellationToken cancellationToken = default) { return await GetMfaStatusAsync(userId, cancellationToken); }

    /// <summary>
    ///     Checks if MFA is required for a user.
    /// </summary>
    public async Task<bool> IsMfaRequiredAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // For now, MFA is required if it's enabled
        // Can be extended to check user roles, policies, etc.
        return await GetMfaStatusAsync(userId, cancellationToken);
    }

    /// <summary>
    ///     Resets failed MFA attempts for a user.
    /// </summary>
    public async Task ResetMfaFailedAttemptsAsync(Guid userId, CancellationToken cancellationToken = default) { await ResetFailedAttemptsAsync(userId, cancellationToken); }

    #endregion
}
