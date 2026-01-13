using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record ProcessSubscriptionRenewalCommand(Guid SubscriptionId) : ICommand;
