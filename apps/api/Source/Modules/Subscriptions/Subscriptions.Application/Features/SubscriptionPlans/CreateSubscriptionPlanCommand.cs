using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;

namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record CreateSubscriptionPlanCommand(
    string Name,
    string Slug,
    long MonthlyPriceInCents,
    string Currency = "USD",
    string? Description = null
) : ICommand<Guid>;

