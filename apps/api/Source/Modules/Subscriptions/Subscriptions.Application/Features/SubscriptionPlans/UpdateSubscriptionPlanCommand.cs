using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;

namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record UpdateSubscriptionPlanCommand(
    Guid Id,
    string Name,
    string? Description,
    int? SortOrder
) : ICommand;

