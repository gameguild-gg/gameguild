namespace GameGuild.Authentication.Commands;

/// <summary>
///     Command to update KYC verification status
/// </summary>
public record UpdateKycVerificationStatusCommand(Guid VerificationId, VerificationStatus Status, string? ResultDetails = null, int? RiskScore = null, string? RejectionReason = null);
