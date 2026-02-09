namespace GameGuild.Identity.Authentication;

/// <summary>
///     Command to update KYC verification status
/// </summary>
public sealed record UpdateKycVerificationStatusCommand(Guid VerificationId, VerificationStatus Status, string? ResultDetails = null, int? RiskScore = null, string? RejectionReason = null);
