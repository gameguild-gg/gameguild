namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Request model for updating plan limits
/// </summary>
public record UpdateLimitsRequest(int? MaxUsers = null, long? MaxStorageMb = null, long? MaxApiCallsPerMonth = null);
