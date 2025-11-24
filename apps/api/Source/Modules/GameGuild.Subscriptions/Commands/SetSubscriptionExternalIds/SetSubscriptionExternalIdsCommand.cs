using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

public record SetSubscriptionExternalIdsCommand(Guid SubscriptionId, string? StripeSubscriptionId, string? PayPalSubscriptionId) : ICommand;
