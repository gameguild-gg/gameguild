using MediatR;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;

namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record UpdateSubscriptionPlanFeaturesCommand(
    Guid Id,
    bool? HasPrioritySupport,
    bool? HasAdvancedAnalytics,
    bool? HasCustomBranding,
    string? Features
) : ICommand;

