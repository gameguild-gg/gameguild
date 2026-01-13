using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record SetSubscriptionExternalIdsCommand(Guid SubscriptionId, string? StripeSubscriptionId, string? PayPalSubscriptionId) : ICommand;
