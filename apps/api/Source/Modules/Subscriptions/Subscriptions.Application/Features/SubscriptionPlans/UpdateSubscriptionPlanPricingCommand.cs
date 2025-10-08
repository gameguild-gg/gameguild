using MediatR;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;

namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record UpdateSubscriptionPlanPricingCommand(
    Guid Id,
    long MonthlyPriceInCents,
    long? AnnualPriceInCents = null
) : ICommand;

