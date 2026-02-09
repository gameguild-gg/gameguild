using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command to update subscription metadata
/// </summary>
public sealed record UpdateSubscriptionMetadataCommand(Guid SubscriptionId, string Metadata) : ICommand;
