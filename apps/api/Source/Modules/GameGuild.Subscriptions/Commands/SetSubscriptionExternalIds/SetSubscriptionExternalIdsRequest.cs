namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Request model for setting subscription external IDs
/// </summary>
public record SetSubscriptionExternalIdsRequest(string? ExternalSubscriptionId, string? ExternalCustomerId);
