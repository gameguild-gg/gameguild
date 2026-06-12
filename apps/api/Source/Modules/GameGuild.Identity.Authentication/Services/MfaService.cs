using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     MFA orchestrator that coordinates TOTP, backup-code, and attempt-tracking sub-services.
///     Kept for backward compatibility with code that depends on IMfaService.
/// </summary>
public sealed class MfaService(
    ILogger<MfaService> logger,
    ITotpMfaService totpMfaService,
    IBackupCodeMfaService backupCodeMfaService,
    IMfaAttemptTrackingService attemptTrackingService,
    IUserMfaConfigurationRepository? mfaConfigRepository = null,
    ISmsService? smsService = null,
    IOptions<SmsMfaOptions>? smsOptions = null) : IMfaService
{
    /// <inheritdoc />
    public Task<MfaConfigurationResponse> GetMfaConfigurationAsync(Guid userId, CancellationToken cancellationToken = default) =>
        attemptTrackingService.GetMfaConfigurationAsync(userId, cancellationToken);

    /// <inheritdoc />
    public async Task<MfaSetupResult> InitiateMfaSetupAsync(Guid userId, string userEmail, CancellationToken cancellationToken = default)
    {
        (var qrCodeUri, var secretKey) = await totpMfaService.SetupTotpAsync(userId, userEmail, cancellationToken).ConfigureAwait(false);

        // Generate backup codes during setup
        var backupCodes = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            backupCodes.Add(backupCodeMfaService.GenerateBackupCode());
        }

        // Store hashed backup codes (before MFA is fully enabled)
        await backupCodeMfaService.StoreBackupCodesForSetupAsync(userId, backupCodes, cancellationToken).ConfigureAwait(false);

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

    /// <inheritdoc />
    public async Task<MfaVerificationResult> CompleteMfaSetupAsync(Guid userId, string totpCode, CancellationToken cancellationToken = default)
    {
        var isValid = await totpMfaService.VerifyTotpAsync(userId, totpCode, null, cancellationToken).ConfigureAwait(false);

        return new MfaVerificationResult { Success = isValid, Message = isValid ? "MFA setup completed successfully" : "Invalid TOTP code" };
    }

    /// <inheritdoc />
    public async Task<SmsMfaSetupResult> InitiateSmsSetupAsync(Guid userId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        if (mfaConfigRepository is null || smsService is null)
        {
            return new SmsMfaSetupResult { Success = false, Message = "SMS MFA is not configured" };
        }

        var normalizedPhoneNumber = NormalizePhoneNumber(phoneNumber);
        if (normalizedPhoneNumber is null)
        {
            return new SmsMfaSetupResult { Success = false, Message = "A valid phone number is required" };
        }

        if (!await smsService.IsConfiguredAsync(cancellationToken).ConfigureAwait(false))
        {
            return new SmsMfaSetupResult { Success = false, Message = "SMS MFA is not available" };
        }

        var options = smsOptions?.Value ?? new SmsMfaOptions();
        var code = GenerateNumericCode(options.CodeLength);
        var now = SystemClock.UtcNow;
        var expiresAt = now.AddSeconds(Math.Max(60, options.CodeExpirationSeconds));
        var configuration = await GetOrCreateMfaConfigurationAsync(userId, now, cancellationToken).ConfigureAwait(false);

        configuration.SmsPhoneNumber = normalizedPhoneNumber;
        configuration.SmsVerificationCodeHash = HashSmsCode(userId, normalizedPhoneNumber, code);
        configuration.SmsVerificationExpiresAt = expiresAt;
        configuration.IsSmsEnabled = false;

        if (configuration.Id == Guid.Empty)
        {
            await mfaConfigRepository.CreateAsync(configuration, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await mfaConfigRepository.UpdateAsync(configuration, cancellationToken).ConfigureAwait(false);
        }

        await smsService.SendVerificationCodeAsync(normalizedPhoneNumber, code, cancellationToken).ConfigureAwait(false);

        return new SmsMfaSetupResult
        {
            Success = true,
            Message = "Verification code sent to your phone number",
            PhoneNumberMasked = MaskPhoneNumber(normalizedPhoneNumber),
            ExpiresInSeconds = (int)(expiresAt - now).TotalSeconds
        };
    }

    /// <inheritdoc />
    public Task<MfaVerificationResult> CompleteSmsSetupAsync(Guid userId, string code, CancellationToken cancellationToken = default) =>
        VerifySmsCodeAsync(userId, code, enableSmsMethod: true, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsSmsMfaAvailableAsync(CancellationToken cancellationToken = default) =>
        smsService?.IsConfiguredAsync(cancellationToken) ?? Task.FromResult(false);

    /// <inheritdoc />
    public async Task<MfaVerificationResult> VerifyMfaAsync(Guid userId, string code, MfaMethod method = MfaMethod.Totp, CancellationToken cancellationToken = default)
    {
        try
        {
            bool isValid;

            if (method == MfaMethod.Totp) { isValid = await totpMfaService.VerifyTotpAsync(userId, code, null, cancellationToken).ConfigureAwait(false); }
            else if (method == MfaMethod.BackupCode) { isValid = await backupCodeMfaService.VerifyBackupCodeAsync(userId, code, null, cancellationToken).ConfigureAwait(false); }
            else if (method == MfaMethod.Sms)
            {
                var smsResult = await VerifySmsCodeAsync(userId, code, enableSmsMethod: false, cancellationToken).ConfigureAwait(false);

                return new MfaVerificationResult
                {
                    Success = smsResult.Success,
                    RequiresAdditionalVerification = false,
                    Message = smsResult.Success ? "MFA verification successful" : "Invalid MFA code"
                };
            }
            else { isValid = false; }

            return new MfaVerificationResult { Success = isValid, RequiresAdditionalVerification = false, Message = isValid ? "MFA verification successful" : "Invalid MFA code" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to verify MFA for user: {UserId}", userId);

            return new MfaVerificationResult { Success = false, Message = ex.Message };
        }
    }

    /// <inheritdoc />
    public async Task<bool> DisableMfaAsync(Guid userId, string confirmationCode, CancellationToken cancellationToken = default)
    {
        // Verify the confirmation code before disabling
        var verificationResult = await VerifyMfaAsync(userId, confirmationCode, MfaMethod.Totp, cancellationToken).ConfigureAwait(false);

        if (!verificationResult.Success) { return false; }

        return await attemptTrackingService.DisableMfaAsync(userId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<string[]> GenerateBackupCodesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        backupCodeMfaService.GenerateBackupCodesAsync(userId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> VerifyBackupCodeAsync(Guid userId, string backupCode, string? deviceId = null, CancellationToken cancellationToken = default) =>
        backupCodeMfaService.VerifyBackupCodeAsync(userId, backupCode, deviceId, cancellationToken);

    /// <inheritdoc />
    public Task<byte[]> GenerateQrCodeAsync(string qrCodeData, CancellationToken cancellationToken = default) =>
        totpMfaService.GenerateQrCodeAsync(qrCodeData, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsMfaEnabledAsync(Guid userId, CancellationToken cancellationToken = default) =>
        attemptTrackingService.GetMfaStatusAsync(userId, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsMfaRequiredAsync(Guid userId, CancellationToken cancellationToken = default) =>
        attemptTrackingService.IsMfaRequiredByPolicyAsync(userId, cancellationToken);

    /// <inheritdoc />
    public async Task ResetMfaFailedAttemptsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await attemptTrackingService.ResetFailedAttemptsAsync(userId, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<bool> IsUserLockedOutAsync(Guid userId, CancellationToken cancellationToken = default) =>
        attemptTrackingService.IsUserLockedOutAsync(userId, cancellationToken);

    private async Task<UserMfaConfiguration> GetOrCreateMfaConfigurationAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        var configuration = await mfaConfigRepository!.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);

        return configuration ?? new UserMfaConfiguration
        {
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
            PreferredMethod = MfaMethod.Totp
        };
    }

    private async Task<MfaVerificationResult> VerifySmsCodeAsync(Guid userId, string code, bool enableSmsMethod, CancellationToken cancellationToken)
    {
        if (mfaConfigRepository is null)
        {
            return MfaVerificationResult.Failure("Invalid MFA code");
        }

        var configuration = await mfaConfigRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (configuration is null ||
            string.IsNullOrWhiteSpace(configuration.SmsPhoneNumber) ||
            string.IsNullOrWhiteSpace(configuration.SmsVerificationCodeHash) ||
            configuration.SmsVerificationExpiresAt is null)
        {
            return MfaVerificationResult.Failure("SMS MFA setup has not been initiated");
        }

        if (configuration.SmsVerificationExpiresAt <= SystemClock.UtcNow)
        {
            return MfaVerificationResult.Failure("SMS verification code expired");
        }

        var expectedHash = HashSmsCode(userId, configuration.SmsPhoneNumber, code);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedHash),
                Encoding.UTF8.GetBytes(configuration.SmsVerificationCodeHash)))
        {
            return MfaVerificationResult.Failure("Invalid SMS verification code");
        }

        var now = SystemClock.UtcNow;
        configuration.LastUsedAt = now;
        configuration.FailedAttempts = 0;
        configuration.LockedOutUntil = null;
        configuration.SmsVerificationCodeHash = null;
        configuration.SmsVerificationExpiresAt = null;

        if (enableSmsMethod)
        {
            configuration.IsEnabled = true;
            configuration.IsSetupComplete = true;
            configuration.IsSmsEnabled = true;
            configuration.PreferredMethod = MfaMethod.Sms;
            configuration.EnabledAt ??= now;
        }

        await mfaConfigRepository.UpdateAsync(configuration, cancellationToken).ConfigureAwait(false);

        return MfaVerificationResult.Successful(enableSmsMethod ? "SMS MFA setup completed successfully" : "MFA verification successful");
    }

    private static string? NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var trimmed = phoneNumber.Trim();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length is < 7 or > 15)
        {
            return null;
        }

        return trimmed.StartsWith("+", StringComparison.Ordinal) ? $"+{digits}" : digits;
    }

    private static string GenerateNumericCode(int requestedLength)
    {
        var length = Math.Clamp(requestedLength, 4, 9);
        var maxExclusive = (int)Math.Pow(10, length);
        var code = RandomNumberGenerator.GetInt32(0, maxExclusive);

        return code.ToString($"D{length}", CultureInfo.InvariantCulture);
    }

    private static string HashSmsCode(Guid userId, string phoneNumber, string code)
    {
        var bytes = Encoding.UTF8.GetBytes($"{userId:N}:{phoneNumber}:{code}");
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }

    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length < 4)
        {
            return "****";
        }

        return $"***-***-{phoneNumber[^4..]}";
    }
}
