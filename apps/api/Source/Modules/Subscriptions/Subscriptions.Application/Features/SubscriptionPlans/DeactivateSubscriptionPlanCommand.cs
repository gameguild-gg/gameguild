using GameGuild.CQRS;


namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record DeactivateSubscriptionPlanCommand(Guid Id) : ICommand;

