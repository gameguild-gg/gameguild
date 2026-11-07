
namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for managing Multi-Factor Authentication (MFA) including TOTP and backup codes
/// </summary>
public interface IMfaService
{
    Task<MfaConfigurationResponse> GetMfaConfigurationAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<MfaSetupResult> InitiateMfaSetupAsync(Guid userId, string userEmail, CancellationToken cancellationToken = default);

    Task<MfaVerificationResult> CompleteMfaSetupAsync(Guid userId, string totpCode, CancellationToken cancellationToken = default);

    Task<MfaVerificationResult> VerifyMfaAsync(Guid userId, string code, MfaMethod method = MfaMethod.Totp, CancellationToken cancellationToken = default);

    /// <summary>Propagates operational failures so step-up transactions can roll back factor side effects.</summary>
    Task<MfaVerificationResult> VerifyMfaForStepUpAsync(Guid userId, string code, MfaMethod method, CancellationToken cancellationToken = default);

    /// <summary>Issues SMS only for the server-resolved first-factor continuation.</summary>
    Task<SmsMfaChallengeResult> SendSmsSignInChallengeAsync(Guid userId, Guid signInChallengeId, CancellationToken cancellationToken);

    /// <summary>Verifies an SMS proof bound to the server-resolved first-factor continuation.</summary>
    Task<MfaVerificationResult> VerifySmsSignInCodeAsync(Guid userId, Guid signInChallengeId, string code, CancellationToken cancellationToken);

    /// <summary>
    ///     Disables MFA after re-verifying the account password (issue #244). The confirmation
    ///     parameter is the account password, not an MFA code.
    /// </summary>
    Task<bool> DisableMfaAsync(Guid userId, string password, CancellationToken cancellationToken = default);

    Task<string[ ]> GenerateBackupCodesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> VerifyBackupCodeAsync(Guid userId, string backupCode, string? deviceId = null, CancellationToken cancellationToken = default);

    Task<byte[ ]> GenerateQrCodeAsync(string qrCodeData, CancellationToken cancellationToken = default);

    Task<bool> IsMfaEnabledAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> IsMfaRequiredAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ResetMfaFailedAttemptsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> IsUserLockedOutAsync(Guid userId, CancellationToken cancellationToken = default);
}
