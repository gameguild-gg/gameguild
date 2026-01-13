using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record DeactivateSubscriptionPlanCommand(Guid Id) : ICommand;
