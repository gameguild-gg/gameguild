using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Commands;

public record DeleteSubscriptionPlanCommand(Guid Id) : ICommand;
