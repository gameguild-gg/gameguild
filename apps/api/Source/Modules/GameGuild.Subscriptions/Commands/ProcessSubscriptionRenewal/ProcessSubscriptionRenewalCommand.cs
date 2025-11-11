using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

public record ProcessSubscriptionRenewalCommand(Guid SubscriptionId) : ICommand;
