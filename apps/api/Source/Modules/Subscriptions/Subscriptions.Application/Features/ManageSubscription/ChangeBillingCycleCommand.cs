using MediatR;
using GameGuild.Shared;
using GameGuild.Modules.Subscriptions.Entities;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;
using GameGuild.Modules.Subscriptions.Models;

namespace GameGuild.Modules.Subscriptions.Features.ManageSubscription;

/// <summary>
///     Command to change billing cycle
/// </summary>
public record ChangeBillingCycleCommand(
    Guid SubscriptionId,
    BillingCycle NewBillingCycle
) : ICommand;

