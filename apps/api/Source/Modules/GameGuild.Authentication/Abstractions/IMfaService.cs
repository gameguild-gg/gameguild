using GameGuild.Authentication.DTOs;
using GameGuild.Authentication.Enums;
using GameGuild.Authentication.Models.Responses;

namespace GameGuild.Authentication.Abstractions;

/// <summary>
///     Service for managing Multi-Factor Authentication (MFA) including TOTP and backup codes
/// </summary>
public interface IMfaService
{
    Task<MfaConfigurationResponse> GetMfaConfigurationAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Models.Responses.MfaSetupResult> InitiateMfaSetupAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Models.Responses.MfaVerificationResult> CompleteMfaSetupAsync(Guid userId, string totpCode, CancellationToken cancellationToken = default);

    Task<Models.Responses.MfaVerificationResult> VerifyMfaAsync(Guid userId, string code, MfaMethod method = MfaMethod.Totp, CancellationToken cancellationToken = default);

    Task<bool> DisableMfaAsync(Guid userId, string confirmationCode, CancellationToken cancellationToken = default);

    Task<string[ ]> GenerateBackupCodesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> VerifyBackupCodeAsync(Guid userId, string backupCode, string? deviceId = null, CancellationToken cancellationToken = default);

    Task<byte[ ]> GenerateQrCodeAsync(string qrCodeData, CancellationToken cancellationToken = default);

    Task<bool> IsMfaEnabledAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> IsMfaRequiredAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ResetMfaFailedAttemptsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> IsUserLockedOutAsync(Guid userId, CancellationToken cancellationToken = default);
}
