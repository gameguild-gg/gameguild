namespace GameGuild.Identity.Authentication;

/// <summary>
///     Command to complete KYC verification
/// </summary>
public record CompleteKycVerificationCommand(Guid VerificationId, bool Approved, string? ResultDetails = null, int? RiskScore = null, string? RejectionReason = null, TimeSpan? ValidityPeriod = null);
