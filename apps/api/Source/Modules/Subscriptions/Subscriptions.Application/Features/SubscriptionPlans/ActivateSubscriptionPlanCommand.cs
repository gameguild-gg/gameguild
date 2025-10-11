using GameGuild.CQRS;


namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record ActivateSubscriptionPlanCommand(Guid Id) : ICommand;

