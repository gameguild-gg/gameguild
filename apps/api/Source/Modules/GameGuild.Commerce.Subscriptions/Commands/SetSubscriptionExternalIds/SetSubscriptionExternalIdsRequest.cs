namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for setting subscription external IDs
/// </summary>
public record SetSubscriptionExternalIdsRequest(string? ExternalSubscriptionId, string? ExternalCustomerId);
