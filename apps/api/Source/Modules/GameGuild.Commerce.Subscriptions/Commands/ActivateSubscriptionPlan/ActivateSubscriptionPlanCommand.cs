using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record ActivateSubscriptionPlanCommand(Guid Id) : ICommand;
