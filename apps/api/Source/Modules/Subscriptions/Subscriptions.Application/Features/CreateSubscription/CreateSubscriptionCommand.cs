using MediatR;
using GameGuild.Shared;
using GameGuild.Modules.Subscriptions.Entities;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;
using GameGuild.Modules.Subscriptions.Models;

namespace GameGuild.Modules.Subscriptions.Features.CreateSubscription;

/// <summary>
///     Command to create a new subscription
/// </summary>
public record CreateSubscriptionCommand(
    Guid TenantId,
    Guid PlanId,
    Guid CreatedByUserId,
    BillingCycle BillingCycle,
    decimal Amount,
    DateTime? StartDate = null,
    int? TrialDays = null
) : ICommand<Guid>;

