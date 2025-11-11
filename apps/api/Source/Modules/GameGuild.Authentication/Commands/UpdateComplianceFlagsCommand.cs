namespace GameGuild.Authentication.Commands;

/// <summary>
///     Command to update compliance flags
/// </summary>
public record UpdateComplianceFlagsCommand(Guid VerificationId, ComplianceFlags Flags);
