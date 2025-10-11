using GameGuild.CQRS;


namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record SetSubscriptionPlanFeaturedCommand(
    Guid Id,
    bool IsFeatured
) : ICommand;

