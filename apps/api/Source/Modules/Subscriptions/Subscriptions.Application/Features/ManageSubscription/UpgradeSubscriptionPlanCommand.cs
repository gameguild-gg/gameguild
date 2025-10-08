using MediatR;
using GameGuild.Modules.Subscriptions.Entities;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;
using GameGuild.Modules.Subscriptions.Models;

namespace GameGuild.Modules.Subscriptions.Features.ManageSubscription;

/// <summary>
///     Command to upgrade subscription plan
/// </summary>
public record UpgradeSubscriptionPlanCommand(
    Guid SubscriptionId,
    Guid NewPlanId,
    DateTime? EffectiveDate = null
) : ICommand<SubscriptionUpgradeResult>;

