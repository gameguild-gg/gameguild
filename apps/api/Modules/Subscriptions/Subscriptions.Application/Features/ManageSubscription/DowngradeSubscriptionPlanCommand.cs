using GameGuild.CQRS;
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

