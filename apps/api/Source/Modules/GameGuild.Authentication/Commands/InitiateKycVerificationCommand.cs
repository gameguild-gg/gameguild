namespace GameGuild.Authentication.Commands;

/// <summary>
///     Command to initiate KYC verification
/// </summary>
public record InitiateKycVerificationCommand(Guid UserId, string Provider, VerificationLevel Level, VerificationType Type, string? InitiatedFromIp = null);

// TODO: Move these enums to appropriate namespace in Domain layer
