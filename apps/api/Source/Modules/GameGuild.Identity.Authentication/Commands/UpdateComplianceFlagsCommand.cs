namespace GameGuild.Identity.Authentication;

/// <summary>
///     Command to update compliance flags
/// </summary>
public sealed record UpdateComplianceFlagsCommand(Guid VerificationId, ComplianceFlags Flags);
