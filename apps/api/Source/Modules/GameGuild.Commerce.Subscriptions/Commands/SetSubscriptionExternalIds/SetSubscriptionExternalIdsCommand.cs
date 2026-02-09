using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record SetSubscriptionExternalIdsCommand(Guid SubscriptionId, string? StripeSubscriptionId, string? PayPalSubscriptionId) : ICommand;
