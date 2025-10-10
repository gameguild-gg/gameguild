using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Entities;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;
using GameGuild.Modules.Subscriptions.Models;

namespace GameGuild.Modules.Subscriptions.Features.ManageSubscription;

/// <summary>
///     Command to downgrade subscription plan
/// </summary>
public record DowngradeSubscriptionPlanCommand(
    Guid SubscriptionId,
    Guid NewPlanId,
    DateTime? EffectiveDate = null
) : ICommand<SubscriptionDowngradeResult>;

