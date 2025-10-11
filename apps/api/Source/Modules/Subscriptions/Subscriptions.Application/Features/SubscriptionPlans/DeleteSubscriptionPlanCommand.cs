using GameGuild.CQRS;


namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record DeleteSubscriptionPlanCommand(Guid Id) : ICommand;

