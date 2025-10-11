using GameGuild.CQRS;


namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record UpdateSubscriptionPlanCommand(
    Guid Id,
    string Name,
    string? Description,
    int? SortOrder
) : ICommand;

