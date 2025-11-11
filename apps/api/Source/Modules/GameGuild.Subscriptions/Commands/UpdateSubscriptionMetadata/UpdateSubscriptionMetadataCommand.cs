using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Command to update subscription metadata
/// </summary>
public record UpdateSubscriptionMetadataCommand(Guid SubscriptionId, string Metadata) : ICommand;
