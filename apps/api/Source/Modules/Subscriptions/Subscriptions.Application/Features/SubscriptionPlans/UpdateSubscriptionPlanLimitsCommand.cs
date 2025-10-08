using MediatR;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;

namespace GameGuild.Modules.Subscriptions.Features.SubscriptionPlans;

public record UpdateSubscriptionPlanLimitsCommand(
    Guid Id,
    int? MaxUsers,
    long? MaxStorageMb,
    long? MaxApiCallsPerMonth
) : ICommand;

