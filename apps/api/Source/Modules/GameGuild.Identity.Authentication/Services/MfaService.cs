using GameGuild.Configuration.ApplicationLayer;
using GameGuild.CQRS;
using GameGuild.Identity.Users;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     MFA orchestrator that coordinates TOTP, backup-code, SMS-OTP, email-OTP, push-challenge
///     and attempt-tracking sub-services. Kept for backward compatibility with code that depends
///     on IMfaService.
/// </summary>
public sealed class MfaService(
    ILogger<MfaService> logger,
    ITotpMfaService totpMfaService,
    IBackupCodeMfaService backupCodeMfaService,
    IMfaAttemptTrackingService attemptTrackingService,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ISmsMfaService? smsMfaService = null,
    IEmailMfaService? emailMfaService = null,
    IPushMfaService? pushMfaService = null,
    MfaOptions? mfaOptions = null,
    IPublisher? eventPublisher = null,
    IMfaEnrollmentConcurrencyGuard? enrollmentGuard = null) : IMfaService
{
    private readonly MfaOptions _mfaOptions = mfaOptions ?? new MfaOptions();

    /// <inheritdoc />
    public Task<SmsMfaChallengeResult> SendSmsSignInChallengeAsync(Guid userId, Guid signInChallengeId, CancellationToken cancellationToken) =>
        smsMfaService is null
            ? Task.FromResult(new SmsMfaChallengeResult { Success = false, PhoneNumberMasked = string.Empty, ExpiresInSeconds = 0, Message = "Invalid MFA challenge" })
            : smsMfaService.SendSignInChallengeAsync(userId, signInChallengeId, cancellationToken);

    /// <inheritdoc />
    public async Task<MfaVerificationResult> VerifySmsSignInCodeAsync(Guid userId, Guid signInChallengeId, string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var valid = smsMfaService is not null &&
            await smsMfaService.VerifySignInCodeAsync(userId, signInChallengeId, code, cancellationToken).ConfigureAwait(false);
        MfaEnrollmentMetrics.RecordVerification(MfaMethod.Sms, valid);
        if (valid)
        {
            await AuthenticationEventDispatch.PublishAsync(eventPublisher,
                new MfaVerificationSucceededEvent(userId, MfaMethod.Sms.ToString(), SystemClock.UtcNow), logger, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await AuthenticationEventDispatch.PublishAsync(eventPublisher,
                new MfaVerificationFailedEvent(userId, MfaMethod.Sms.ToString(), "Invalid MFA proof", SystemClock.UtcNow), logger, cancellationToken).ConfigureAwait(false);
        }
        return new MfaVerificationResult { Success = valid, Message = valid ? "MFA verification successful" : "Invalid MFA code" };
    }

    /// <inheritdoc />
    public Task<MfaConfigurationResponse> GetMfaConfigurationAsync(Guid userId, CancellationToken cancellationToken = default) =>
        attemptTrackingService.GetMfaConfigurationAsync(userId, cancellationToken);

    /// <inheritdoc />
    public async Task<MfaSetupResult> InitiateMfaSetupAsync(Guid userId, string userEmail, CancellationToken cancellationToken = default)
    {
        MfaEnrollmentMetrics.RecordEnrollment(MfaMethod.Totp, "started");
        (string QrCodeUri, string SecretKey, DateTimeOffset ExpiresAt) setup;
        try
        {
            setup = await totpMfaService.SetupTotpAsync(userId, userEmail, cancellationToken).ConfigureAwait(false);
        }
        catch (MfaConfigurationDeniedException exception)
        {
            return new MfaSetupResult { Success = false, Message = exception.Message };
        }

        var (qrCodeUri, secretKey, expiresAt) = setup;

        // Generate backup codes during setup
        var backupCodes = new List<string>();
        for (var i = 0; i < _mfaOptions.BackupCodesCount; i++)
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
            ExpiresAt = expiresAt,
            BackupCodes = backupCodes.ToArray(),
            Message = "MFA setup initiated successfully"
        };
    }

    /// <inheritdoc />
    public async Task<MfaVerificationResult> CompleteMfaSetupAsync(Guid userId, string totpCode, CancellationToken cancellationToken = default)
    {
        var isValid = await totpMfaService.VerifyTotpAsync(userId, totpCode, null, cancellationToken).ConfigureAwait(false);
        MfaEnrollmentMetrics.RecordEnrollment(MfaMethod.Totp, isValid ? "enrolled" : "verify-failed");

        // In-process MFA lifecycle telemetry (issue #1199): the first successful TOTP
        // verification is the instant the factor becomes enabled; a failed one is a
        // verification failure during enrollment.
        if (isValid)
        {
            await AuthenticationEventDispatch.PublishAsync(
                eventPublisher,
                new MfaEnabledEvent(userId, MfaMethod.Totp.ToString(), SystemClock.UtcNow),
                logger,
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await AuthenticationEventDispatch.PublishAsync(
                eventPublisher,
                new MfaVerificationFailedEvent(userId, MfaMethod.Totp.ToString(), "Enrollment verification failed", SystemClock.UtcNow),
                logger,
                cancellationToken).ConfigureAwait(false);
        }

        return new MfaVerificationResult { Success = isValid, Message = isValid ? "MFA setup completed successfully" : "Invalid TOTP code" };
    }

    /// <inheritdoc />
    public Task<MfaVerificationResult> VerifyMfaAsync(Guid userId, string code, MfaMethod method = MfaMethod.Totp, CancellationToken cancellationToken = default)
        => VerifyMfaCoreAsync(userId, code, method, false, cancellationToken);

    public Task<MfaVerificationResult> VerifyMfaForStepUpAsync(Guid userId, string code, MfaMethod method, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (method is not (MfaMethod.Totp or MfaMethod.BackupCode))
        {
            throw new ArgumentOutOfRangeException(nameof(method));
        }
        return VerifyMfaCoreAsync(userId, code, method, true, cancellationToken);
    }

    private async Task<MfaVerificationResult> VerifyMfaCoreAsync(Guid userId, string code, MfaMethod method,
        bool propagateOperationalFailures, CancellationToken cancellationToken)
    {
        try
        {
            MfaVerificationResult result;

            if (method == MfaMethod.Push)
            {
                // Push verification is challenge-id driven (no typed code); delegate to the
                // push service which resolves the user's outstanding challenge and its TTL.
                result = pushMfaService is null
                    ? new MfaVerificationResult { Success = false, Message = "Invalid MFA code" }
                    : await pushMfaService.VerifyAsync(userId, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                bool isValid;

                if (method == MfaMethod.Totp)
                {
                    isValid = propagateOperationalFailures
                        ? await totpMfaService.VerifyTotpForStepUpAsync(userId, code, null, cancellationToken).ConfigureAwait(false)
                        : await totpMfaService.VerifyTotpAsync(userId, code, null, cancellationToken).ConfigureAwait(false);
                }
                else if (method == MfaMethod.BackupCode)
                {
                    isValid = propagateOperationalFailures
                        ? await backupCodeMfaService.VerifyBackupCodeForStepUpAsync(userId, code, null, cancellationToken).ConfigureAwait(false)
                        : await backupCodeMfaService.VerifyBackupCodeAsync(userId, code, null, cancellationToken).ConfigureAwait(false);
                }
                else if (method == MfaMethod.Sms)
                {
                    if (smsMfaService is null) { isValid = false; }
                    else { isValid = await smsMfaService.VerifyCodeAsync(userId, code, cancellationToken).ConfigureAwait(false); }
                }
                else if (method == MfaMethod.Email)
                {
                    if (emailMfaService is null) { isValid = false; }
                    else { isValid = await emailMfaService.VerifyCodeAsync(userId, code, cancellationToken).ConfigureAwait(false); }
                }
                else { isValid = false; }

                MfaEnrollmentMetrics.RecordVerification(method, isValid);
                result = new MfaVerificationResult
                {
                    Success = isValid,
                    RequiresAdditionalVerification = false,
                    Message = isValid ? "MFA verification successful" : "Invalid MFA code"
                };
            }

            // In-process MFA verification telemetry (issue #1199).
            if (result.Success)
            {
                await AuthenticationEventDispatch.PublishAsync(
                    eventPublisher,
                    new MfaVerificationSucceededEvent(userId, method.ToString(), SystemClock.UtcNow),
                    logger,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await AuthenticationEventDispatch.PublishAsync(
                    eventPublisher,
                    new MfaVerificationFailedEvent(userId, method.ToString(), result.Message ?? "Invalid MFA code", SystemClock.UtcNow),
                    logger,
                    cancellationToken).ConfigureAwait(false);
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (!propagateOperationalFailures)
        {
            logger.LogError(ex, "Failed to verify MFA for user: {UserId}", userId);

            await AuthenticationEventDispatch.PublishAsync(
                eventPublisher,
                new MfaVerificationFailedEvent(userId, method.ToString(), ex.Message ?? "MFA verification failed", SystemClock.UtcNow),
                logger,
                cancellationToken).ConfigureAwait(false);

            return new MfaVerificationResult { Success = false, Message = ex.Message };
        }
    }

    /// <inheritdoc />
    public async Task<bool> DisableMfaAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        // Password re-verification before disabling (issue #244): possession of a live TOTP code
        // alone must not be enough to strip the account's factors — the primary credential is
        // required, and passwordless accounts (no stored hash) cannot disable MFA this way.
        var user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user?.PasswordHash is not { } passwordHash)
        {
            logger.LogWarning("MFA disable refused for user {UserId}: no password credential on file", userId);
            MfaEnrollmentMetrics.RecordEnrollment(MfaMethod.Totp, "disable-refused");
            return false;
        }

        if (!passwordHasher.VerifyPassword(passwordHash, password))
        {
            logger.LogWarning("MFA disable refused for user {UserId}: password verification failed", userId);
            MfaEnrollmentMetrics.RecordEnrollment(MfaMethod.Totp, "disable-refused");
            return false;
        }

        // The disable flips the same configuration row enrollment writes (issue #1117):
        // hold the subject's enrollment claim for the read-modify-write so a concurrent
        // enrollment (settings setup, sign-in-bound provisioning, first-verify) cannot
        // interleave with the teardown.
        var guard = enrollmentGuard;
        if (guard is null)
        {
            return await DisableEnrolledFactorAsync(userId, cancellationToken).ConfigureAwait(false);
        }

        return await guard.ExecuteEnrollmentOperationAsync(
            userId,
            () => DisableEnrolledFactorAsync(userId, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> DisableEnrolledFactorAsync(Guid userId, CancellationToken cancellationToken)
    {
        var disabled = await attemptTrackingService.DisableMfaAsync(userId, cancellationToken).ConfigureAwait(false);
        if (disabled)
        {
            MfaEnrollmentMetrics.RecordEnrollment(MfaMethod.Totp, "disabled");

            // In-process MFA lifecycle telemetry (issue #1199): the factor is confirmed disabled.
            await AuthenticationEventDispatch.PublishAsync(
                eventPublisher,
                new MfaDisabledEvent(userId, MfaMethod.Totp.ToString(), SystemClock.UtcNow),
                logger,
                cancellationToken).ConfigureAwait(false);
        }

        return disabled;
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
}
