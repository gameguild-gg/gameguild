using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

public sealed record DeleteSubscriptionPlanCommand(Guid Id) : ICommand;
