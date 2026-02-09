using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record DeactivateSubscriptionPlanCommand(Guid Id) : ICommand;
