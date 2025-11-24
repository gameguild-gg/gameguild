using GameGuild.CQRS;
using GameGuild.Subscriptions.Models;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Command to upgrade subscription plan
/// </summary>
public record UpgradeSubscriptionPlanCommand(Guid SubscriptionId, Guid NewPlanId, DateTime? EffectiveDate = null) : ICommand<SubscriptionUpgradeResult>;
