using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public record DeleteSubscriptionPlanCommand(Guid Id) : ICommand;
