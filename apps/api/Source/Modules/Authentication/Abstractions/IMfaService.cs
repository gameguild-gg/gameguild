namespace GameGuild.Modules.Authentication;

/// <summary>
/// Service for managing Multi-Factor Authentication (MFA) including TOTP and backup codes
/// </summary>
public interface IMfaService
{
    Task<MfaSetupResult> InitiateMfaSetupAsync(Guid userId);

    Task<MfaVerificationResult> CompleteMfaSetupAsync(Guid userId, string totpCode);

    Task<MfaVerificationResult> VerifyMfaAsync(Guid userId, string code, MfaMethod method = MfaMethod.Totp);

    Task<bool> DisableMfaAsync(Guid userId, string confirmationCode);

    Task<string[ ]> GenerateBackupCodesAsync(Guid userId);

    Task<byte[ ]> GenerateQrCodeAsync(string qrCodeData);

    Task<bool> IsMfaEnabledAsync(Guid userId);

    Task<bool> IsMfaRequiredAsync(Guid userId);

    Task ResetMfaFailedAttemptsAsync(Guid userId);

    Task<bool> IsUserLockedOutAsync(Guid userId);
}
