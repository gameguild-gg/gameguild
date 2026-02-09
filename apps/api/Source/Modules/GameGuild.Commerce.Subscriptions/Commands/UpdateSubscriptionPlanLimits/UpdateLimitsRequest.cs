namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for updating plan limits
/// </summary>
public sealed record UpdateLimitsRequest(int? MaxUsers = null, long? MaxStorageMb = null, long? MaxApiCallsPerMonth = null);
