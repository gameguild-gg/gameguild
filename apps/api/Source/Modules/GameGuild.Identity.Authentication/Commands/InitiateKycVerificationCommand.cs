namespace GameGuild.Identity.Authentication;

/// <summary>
///     Command to initiate KYC verification
/// </summary>
public sealed record InitiateKycVerificationCommand(Guid UserId, string Provider, VerificationLevel Level, VerificationType Type, string? InitiatedFromIp = null);

// PLANNED: Move VerificationLevel and VerificationType enums to a shared Domain/Compliance namespace
